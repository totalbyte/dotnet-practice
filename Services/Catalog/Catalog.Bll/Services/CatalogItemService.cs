using AutoMapper;
using Catalog.Bll.Diagnostics;
using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Specifications;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Catalog.Bll.Services;

public sealed class CatalogItemService(IUnitOfWork uow, IMapper mapper, ILogger<CatalogItemService> logger)
    : ICatalogItemService
{
    public async Task<PagedResult<CatalogItemDto>> GetListAsync(
        CatalogItemQuery query, CancellationToken ct = default)
    {
        var pageSize = query.EffectivePageSize();
        var specification = new CatalogItemsFilterSpec(query, pageSize);

        var totalCount = await uow.CatalogItems.CountAsync(specification, ct);
        var items = await uow.CatalogItems.ListAsync(specification, ct);

        return new PagedResult<CatalogItemDto>(
            mapper.Map<List<CatalogItemDto>>(items), totalCount, query.Page, pageSize);
    }

    public async Task<CatalogItemDetailsDto> GetByIdAsync(long id, CancellationToken ct = default)
        => mapper.Map<CatalogItemDetailsDto>(await FindWithDetailsAsync(id, ct));

    public async Task<IReadOnlyList<CatalogItemDetailsDto>> GetByIdsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return [];

        var items = await uow.CatalogItems.ListAsync(new CatalogItemsByIdsSpec(ids), ct);
        return mapper.Map<List<CatalogItemDetailsDto>>(items);
    }

    public async Task<CatalogItemDetailsDto> CreateAsync(CreateCatalogItemDto input, CancellationToken ct = default)
    {
        using var activity = CatalogTelemetry.ActivitySource.StartActivity("CreateCatalogItem");
        activity?.SetTag("catalog_item.sku", input.Sku);

        if (await uow.CatalogItems.ExistsBySkuAsync(input.Sku, ct))
            throw new ConflictException($"Catalog item with SKU '{input.Sku}' already exists");
        await EnsureBrandExistsAsync(input.BrandId, ct);

        var item = new CatalogItem(
            input.Sku, input.Name, input.ItemType!.Value, input.Price, input.BrandId, input.CreatedBy);
        if (input.Details is { } details)
            item.Describe(
                details.Description, details.WarrantyMonths, details.EstimatedDurationMinutes,
                details.Specifications);

        await uow.CatalogItems.AddAsync(item, ct);
        await uow.SaveChangesAsync(ct);

        activity?.SetTag("catalog_item.id", item.Id);
        logger.LogInformation("Catalog item {CatalogItemId} created with SKU {Sku}, type {ItemType}, price {Price}",
            item.Id, item.Sku, item.ItemType, item.Price);
        return mapper.Map<CatalogItemDetailsDto>(await FindWithDetailsAsync(item.Id, ct));
    }

    public async Task UpdateAsync(long id, UpdateCatalogItemDto input, CancellationToken ct = default)
    {
        using var activity = CatalogTelemetry.ActivitySource.StartActivity("UpdateCatalogItem");
        activity?.SetTag("catalog_item.id", id);

        var item = await FindWithDetailsAsync(id, ct);
        if (input.BrandId != item.BrandId)
            await EnsureBrandExistsAsync(input.BrandId, ct);

        item.Rename(input.Name, input.UpdatedBy);
        item.AssignBrand(input.BrandId, input.UpdatedBy);
        if (input.IsActive!.Value)
            item.Activate(input.UpdatedBy);
        else
            item.Deactivate(input.UpdatedBy);
        if (input.Details is { } details)
            item.Describe(
                details.Description, details.WarrantyMonths, details.EstimatedDurationMinutes,
                details.Specifications, input.UpdatedBy);

        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Catalog item {CatalogItemId} updated, active {IsActive}", id, item.IsActive);
    }

    public async Task<CatalogItemDetailsDto> ChangePriceAsync(
        long id, ChangePriceDto input, CancellationToken ct = default)
    {
        using var activity = CatalogTelemetry.ActivitySource.StartActivity("ChangeCatalogItemPrice");
        activity?.SetTag("catalog_item.id", id);

        var item = await FindWithDetailsAsync(id, ct);
        var oldPrice = item.Price;

        item.ChangePrice(input.Price, input.UpdatedBy);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Catalog item {CatalogItemId} price changed from {OldPrice} to {NewPrice}", id, oldPrice, item.Price);
        return mapper.Map<CatalogItemDetailsDto>(item);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var item = await uow.CatalogItems.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(CatalogItem), id);

        item.MarkDeleted();
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Catalog item {CatalogItemId} marked as deleted", id);
    }

    public async Task AssignCategoryAsync(
        long id, int categoryId, AssignCategoryDto input, CancellationToken ct = default)
    {
        var item = await FindWithDetailsAsync(id, ct);
        if (await uow.Categories.GetByIdAsync(categoryId, ct) is null)
            throw new EntityNotFoundException(nameof(Category), categoryId);

        item.AssignCategory(categoryId, input.IsPrimary);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Catalog item {CatalogItemId} assigned to category {CategoryId}, primary: {IsPrimary}",
            id, categoryId, input.IsPrimary);
    }

    public async Task RemoveCategoryAsync(long id, int categoryId, CancellationToken ct = default)
    {
        var item = await FindWithDetailsAsync(id, ct);
        if (item.Categories.All(c => c.CategoryId != categoryId))
            throw new EntityNotFoundException(nameof(Category), categoryId);

        item.RemoveFromCategory(categoryId);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Catalog item {CatalogItemId} removed from category {CategoryId}", id, categoryId);
    }

    private async Task<CatalogItem> FindWithDetailsAsync(long id, CancellationToken ct)
        => await uow.CatalogItems.GetWithDetailsAsync(id, ct)
           ?? throw new EntityNotFoundException(nameof(CatalogItem), id);

    private async Task EnsureBrandExistsAsync(int? brandId, CancellationToken ct)
    {
        if (brandId is { } id && await uow.Brands.GetByIdAsync(id, ct) is null)
            throw new ValidationException(nameof(CreateCatalogItemDto.BrandId), $"Brand '{id}' does not exist");
    }
}

using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Dtos.Categories;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Specifications;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Catalog.Bll.Services;

public sealed class CategoryService(IUnitOfWork uow, IMapper mapper, ILogger<CategoryService> logger)
    : ICategoryService
{
    public async Task<PagedResult<CategoryDto>> GetListAsync(CategoryQuery query, CancellationToken ct = default)
    {
        var pageSize = query.EffectivePageSize();
        var specification = new CategoriesFilterSpec(query, pageSize);

        var totalCount = await uow.Categories.CountAsync(specification, ct);
        var categories = await uow.Categories.ListAsync(specification, ct);

        return new PagedResult<CategoryDto>(
            mapper.Map<List<CategoryDto>>(categories), totalCount, query.Page, pageSize);
    }

    public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken ct = default)
        => mapper.Map<CategoryDto>(await FindAsync(id, ct));

    public async Task<CategoryWithItemsDto> GetWithItemsAsync(int id, CancellationToken ct = default)
    {
        var category = await uow.Categories.GetWithItemsAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);

        return mapper.Map<CategoryWithItemsDto>(category);
    }

    public async Task<IReadOnlyList<CatalogItemSummaryDto>> GetRecentItemsAsync(
        string slug, DateTime since, CancellationToken ct = default)
    {
        if (!await uow.Categories.AnyAsync(new CategoryBySlugSpec(slug), ct))
            throw new EntityNotFoundException(nameof(Category), slug);

        var items = await uow.CatalogItemCategories.GetRecentlyAddedAsync(slug, since, ct);
        return mapper.Map<List<CatalogItemSummaryDto>>(items);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto input, CancellationToken ct = default)
    {
        var category = new Category(input.Name, input.Slug, input.ParentCategoryId, input.DisplayOrder);

        await EnsureUniqueAsync(category, null, ct);
        await EnsureParentIsValidAsync(category, ct);

        await uow.Categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Category {CategoryId} created with slug {Slug}", category.Id, category.Slug);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task UpdateAsync(int id, UpdateCategoryDto input, CancellationToken ct = default)
    {
        var category = await FindAsync(id, ct);

        category.Rename(input.Name);
        category.ChangeSlug(input.Slug);
        category.MoveTo(input.ParentCategoryId);
        category.ChangeDisplayOrder(input.DisplayOrder);

        await EnsureUniqueAsync(category, id, ct);
        await EnsureParentIsValidAsync(category, ct);

        uow.Categories.Update(category);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Category {CategoryId} updated", id);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await FindAsync(id, ct);

        if (await uow.Categories.AnyAsync(new CategoryChildrenSpec(id), ct))
            throw new ConflictException($"Category '{category.Slug}' has subcategories");

        uow.Categories.Delete(category);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Category {CategoryId} deleted", id);
    }

    private async Task<Category> FindAsync(int id, CancellationToken ct)
        => await uow.Categories.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException(nameof(Category), id);

    private async Task EnsureUniqueAsync(Category category, int? exceptId, CancellationToken ct)
    {
        if (await uow.Categories.AnyAsync(new CategoryDuplicateSpec(category.Name, category.Slug, exceptId), ct))
            throw new ConflictException(
                $"Category with name '{category.Name}' or slug '{category.Slug}' already exists");
    }

    private async Task EnsureParentIsValidAsync(Category category, CancellationToken ct)
    {
        var parentId = category.ParentCategoryId;
        while (parentId is not null)
        {
            var parent = await uow.Categories.GetByIdAsync(parentId.Value, ct)
                ?? throw new ValidationException(
                    nameof(CreateCategoryDto.ParentCategoryId), $"Category '{parentId}' does not exist");

            if (category.Id != 0 && parent.ParentCategoryId == category.Id)
                throw new ConflictException("Category cannot be moved under its own subcategory");

            parentId = parent.ParentCategoryId;
        }
    }
}

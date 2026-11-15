using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.Brands;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Specifications;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Catalog.Bll.Services;

public sealed class BrandService(IUnitOfWork uow, IMapper mapper, ILogger<BrandService> logger) : IBrandService
{
    public async Task<PagedResult<BrandDto>> GetListAsync(BrandQuery query, CancellationToken ct = default)
    {
        var pageSize = query.EffectivePageSize();
        var specification = new BrandsFilterSpec(query, pageSize);

        var totalCount = await uow.Brands.CountAsync(specification, ct);
        var brands = await uow.Brands.ListAsync(specification, ct);

        return new PagedResult<BrandDto>(mapper.Map<List<BrandDto>>(brands), totalCount, query.Page, pageSize);
    }

    public async Task<BrandDto> GetByIdAsync(int id, CancellationToken ct = default)
        => mapper.Map<BrandDto>(await FindAsync(id, ct));

    public async Task<BrandDto> CreateAsync(CreateBrandDto input, CancellationToken ct = default)
    {
        var brand = new Brand(input.Name, input.Country);
        await EnsureNameIsFreeAsync(brand.Name, null, ct);

        await uow.Brands.AddAsync(brand, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Brand {BrandId} created with name {BrandName}", brand.Id, brand.Name);
        return mapper.Map<BrandDto>(brand);
    }

    public async Task UpdateAsync(int id, UpdateBrandDto input, CancellationToken ct = default)
    {
        var brand = await FindAsync(id, ct);

        brand.Rename(input.Name);
        brand.ChangeCountry(input.Country);
        await EnsureNameIsFreeAsync(brand.Name, id, ct);

        uow.Brands.Update(brand);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Brand {BrandId} updated", id);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var brand = await FindAsync(id, ct);

        if (await uow.CatalogItems.AnyAsync(new CatalogItemsByBrandSpec(id), ct))
            throw new ConflictException($"Brand '{brand.Name}' is used by catalog items");

        uow.Brands.Delete(brand);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Brand {BrandId} deleted", id);
    }

    private async Task<Brand> FindAsync(int id, CancellationToken ct)
        => await uow.Brands.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException(nameof(Brand), id);

    private async Task EnsureNameIsFreeAsync(string name, int? exceptId, CancellationToken ct)
    {
        if (await uow.Brands.AnyAsync(new BrandByNameSpec(name, exceptId), ct))
            throw new ConflictException($"Brand with name '{name}' already exists");
    }
}

using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.Brands;
using Catalog.Bll.Interfaces;
using Microsoft.Extensions.Options;

namespace Catalog.Bll.Caching;

public sealed class CachedBrandService(
    IBrandService inner,
    TwoLevelCache cache,
    CatalogCacheInvalidator invalidator,
    IOptions<CatalogCacheOptions> options) : IBrandService
{
    private CatalogCacheOptions Options => options.Value;

    public async Task<PagedResult<BrandDto>> GetListAsync(BrandQuery query, CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(CatalogCacheKeys.BrandListsVersion, Options.ListVersions, ct);
        return await cache.GetOrCreateAsync(CatalogCacheKeys.BrandList(version, query), Options.ReferenceData,
            token => inner.GetListAsync(query, token), ct);
    }

    public Task<BrandDto> GetByIdAsync(int id, CancellationToken ct = default)
        => cache.GetOrCreateAsync(CatalogCacheKeys.Brand(id), Options.ReferenceData,
            token => inner.GetByIdAsync(id, token), ct);

    public async Task<BrandDto> CreateAsync(CreateBrandDto input, CancellationToken ct = default)
    {
        var created = await inner.CreateAsync(input, ct);
        await invalidator.BrandCreatedAsync("BrandCreated", ct);
        return created;
    }

    public async Task UpdateAsync(int id, UpdateBrandDto input, CancellationToken ct = default)
    {
        await inner.UpdateAsync(id, input, ct);
        await invalidator.BrandChangedAsync(id, "BrandUpdated", ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await invalidator.BrandChangedAsync(id, "BrandDeleted", ct);
    }
}

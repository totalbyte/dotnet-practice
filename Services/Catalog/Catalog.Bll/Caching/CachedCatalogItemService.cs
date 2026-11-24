using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Interfaces;
using Microsoft.Extensions.Options;

namespace Catalog.Bll.Caching;

public sealed class CachedCatalogItemService(
    ICatalogItemService inner,
    TwoLevelCache cache,
    CatalogCacheInvalidator invalidator,
    IOptions<CatalogCacheOptions> options) : ICatalogItemService
{
    private CatalogCacheOptions Options => options.Value;

    public async Task<PagedResult<CatalogItemDto>> GetListAsync(CatalogItemQuery query, CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(CatalogCacheKeys.ItemListsVersion, Options.ListVersions, ct);
        return await cache.GetOrCreateAsync(CatalogCacheKeys.ItemList(version, query), Options.Lists,
            token => inner.GetListAsync(query, token), ct);
    }

    public Task<CatalogItemDetailsDto> GetByIdAsync(long id, CancellationToken ct = default)
        => cache.GetOrCreateAsync(CatalogCacheKeys.Item(id), Options.Items, token => inner.GetByIdAsync(id, token), ct);

    public Task<IReadOnlyList<CatalogItemDetailsDto>> GetByIdsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct = default)
        => cache.GetOrCreateManyAsync(ids, CatalogCacheKeys.Item, item => item.Id, Options.Items,
            inner.GetByIdsAsync, ct);

    public async Task<CatalogItemDetailsDto> CreateAsync(CreateCatalogItemDto input, CancellationToken ct = default)
    {
        var created = await inner.CreateAsync(input, ct);
        await invalidator.ItemCreatedAsync("CatalogItemCreated", ct);
        return created;
    }

    public async Task UpdateAsync(long id, UpdateCatalogItemDto input, CancellationToken ct = default)
    {
        await inner.UpdateAsync(id, input, ct);
        await invalidator.ItemChangedAsync(id, "CatalogItemUpdated", ct);
    }

    public async Task<CatalogItemDetailsDto> ChangePriceAsync(
        long id, ChangePriceDto input, CancellationToken ct = default)
    {
        var changed = await inner.ChangePriceAsync(id, input, ct);
        await invalidator.ItemChangedAsync(id, "CatalogItemPriceChanged", ct);
        return changed;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await invalidator.ItemChangedAsync(id, "CatalogItemDeleted", ct);
    }

    public async Task AssignCategoryAsync(
        long id, int categoryId, AssignCategoryDto input, CancellationToken ct = default)
    {
        await inner.AssignCategoryAsync(id, categoryId, input, ct);
        await invalidator.ItemChangedAsync(id, "CatalogItemCategoryAssigned", ct);
    }

    public async Task RemoveCategoryAsync(long id, int categoryId, CancellationToken ct = default)
    {
        await inner.RemoveCategoryAsync(id, categoryId, ct);
        await invalidator.ItemChangedAsync(id, "CatalogItemCategoryRemoved", ct);
    }
}

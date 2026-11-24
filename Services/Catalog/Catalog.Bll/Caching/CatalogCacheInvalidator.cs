using Catalog.Bll.Specifications;
using Catalog.Dal.Interfaces;
using Microsoft.Extensions.Options;

namespace Catalog.Bll.Caching;

public sealed class CatalogCacheInvalidator(TwoLevelCache cache, IUnitOfWork uow, IOptions<CatalogCacheOptions> options)
{
    private CacheEntryPolicy Versions => options.Value.ListVersions;

    public Task ItemCreatedAsync(string reason, CancellationToken ct)
        => cache.ChangeVersionAsync(CatalogCacheKeys.ItemListsVersion, Versions, reason, ct);

    public async Task ItemChangedAsync(long id, string reason, CancellationToken ct)
    {
        await cache.RemoveAsync(CatalogCacheKeys.Item(id), reason, ct);
        await cache.ChangeVersionAsync(CatalogCacheKeys.ItemListsVersion, Versions, reason, ct);
    }

    public Task CategoryCreatedAsync(string reason, CancellationToken ct)
        => cache.ChangeVersionAsync(CatalogCacheKeys.CategoryListsVersion, Versions, reason, ct);

    public async Task<IReadOnlyList<long>> GetItemIdsInCategoryAsync(int categoryId, CancellationToken ct)
        => (await uow.Categories.GetWithItemsAsync(categoryId, ct))?.ItemCategories
               .Select(ic => ic.CatalogItemId).ToList()
           ?? [];

    public async Task CategoryChangedAsync(
        int id, IReadOnlyCollection<long> affectedItemIds, string reason, CancellationToken ct)
    {
        await cache.RemoveAsync(CatalogCacheKeys.Category(id), reason, ct);
        await cache.ChangeVersionAsync(CatalogCacheKeys.CategoryListsVersion, Versions, reason, ct);
        await RemoveItemsAsync(affectedItemIds, reason, ct);
    }

    public Task BrandCreatedAsync(string reason, CancellationToken ct)
        => cache.ChangeVersionAsync(CatalogCacheKeys.BrandListsVersion, Versions, reason, ct);

    public async Task BrandChangedAsync(int id, string reason, CancellationToken ct)
    {
        await cache.RemoveAsync(CatalogCacheKeys.Brand(id), reason, ct);
        await cache.ChangeVersionAsync(CatalogCacheKeys.BrandListsVersion, Versions, reason, ct);

        var itemIds = (await uow.CatalogItems.ListAsync(new CatalogItemsByBrandSpec(id), ct)).Select(i => i.Id).ToList();
        await RemoveItemsAsync(itemIds, reason, ct);
    }

    private async Task RemoveItemsAsync(IReadOnlyCollection<long> itemIds, string reason, CancellationToken ct)
    {
        if (itemIds.Count == 0)
            return;

        foreach (var itemId in itemIds)
            await cache.RemoveAsync(CatalogCacheKeys.Item(itemId), reason, ct);

        await cache.ChangeVersionAsync(CatalogCacheKeys.ItemListsVersion, Versions, reason, ct);
    }
}

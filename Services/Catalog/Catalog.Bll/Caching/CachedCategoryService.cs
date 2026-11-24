using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Dtos.Categories;
using Catalog.Bll.Interfaces;
using Microsoft.Extensions.Options;

namespace Catalog.Bll.Caching;

public sealed class CachedCategoryService(
    ICategoryService inner,
    TwoLevelCache cache,
    CatalogCacheInvalidator invalidator,
    IOptions<CatalogCacheOptions> options) : ICategoryService
{
    private CatalogCacheOptions Options => options.Value;

    public async Task<PagedResult<CategoryDto>> GetListAsync(CategoryQuery query, CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(CatalogCacheKeys.CategoryListsVersion, Options.ListVersions, ct);
        return await cache.GetOrCreateAsync(CatalogCacheKeys.CategoryList(version, query), Options.ReferenceData,
            token => inner.GetListAsync(query, token), ct);
    }

    public Task<CategoryDto> GetByIdAsync(int id, CancellationToken ct = default)
        => cache.GetOrCreateAsync(CatalogCacheKeys.Category(id), Options.ReferenceData,
            token => inner.GetByIdAsync(id, token), ct);

    public Task<CategoryWithItemsDto> GetWithItemsAsync(int id, CancellationToken ct = default)
        => inner.GetWithItemsAsync(id, ct);

    public Task<IReadOnlyList<CatalogItemSummaryDto>> GetRecentItemsAsync(
        string slug, DateTime since, CancellationToken ct = default)
        => inner.GetRecentItemsAsync(slug, since, ct);

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto input, CancellationToken ct = default)
    {
        var created = await inner.CreateAsync(input, ct);
        await invalidator.CategoryCreatedAsync("CategoryCreated", ct);
        return created;
    }

    public async Task UpdateAsync(int id, UpdateCategoryDto input, CancellationToken ct = default)
    {
        var itemIds = await invalidator.GetItemIdsInCategoryAsync(id, ct);
        await inner.UpdateAsync(id, input, ct);
        await invalidator.CategoryChangedAsync(id, itemIds, "CategoryUpdated", ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var itemIds = await invalidator.GetItemIdsInCategoryAsync(id, ct);
        await inner.DeleteAsync(id, ct);
        await invalidator.CategoryChangedAsync(id, itemIds, "CategoryDeleted", ct);
    }
}

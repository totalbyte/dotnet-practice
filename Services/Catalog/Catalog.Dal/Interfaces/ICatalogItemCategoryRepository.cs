using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface ICatalogItemCategoryRepository
    : IGenericRepository<CatalogItemCategory, (long CatalogItemId, int CategoryId)>
{
    Task<IReadOnlyList<CatalogItem>> GetRecentlyAddedAsync(
        string categorySlug, DateTime since, CancellationToken ct = default);
}

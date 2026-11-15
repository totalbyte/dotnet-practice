using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface ICatalogItemRepository : IGenericRepository<CatalogItem, long>
{
    Task<CatalogItem?> GetWithDetailsAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default);
}

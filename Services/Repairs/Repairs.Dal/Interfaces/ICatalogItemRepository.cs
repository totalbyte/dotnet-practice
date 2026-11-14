using Repairs.Domain.Models;

namespace Repairs.Dal.Interfaces;

public interface ICatalogItemRepository : IGenericRepository<CatalogItem>
{
    Task<bool> DecreaseStockAsync(long id, int quantity, CancellationToken ct = default);
    Task<bool> SetStockAsync(long id, int stock, CancellationToken ct = default);
}

using Repairs.Bll.Dtos.CatalogItems;

namespace Repairs.Bll.Interfaces;

public interface ICatalogItemService
{
    Task<IReadOnlyList<CatalogItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<CatalogItemDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task UpdateStockAsync(long id, UpdateStockDto input, CancellationToken ct = default);
}

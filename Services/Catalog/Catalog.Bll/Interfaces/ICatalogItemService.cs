using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.CatalogItems;

namespace Catalog.Bll.Interfaces;

public interface ICatalogItemService
{
    Task<PagedResult<CatalogItemDto>> GetListAsync(CatalogItemQuery query, CancellationToken ct = default);
    Task<CatalogItemDetailsDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogItemDetailsDto>> GetByIdsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct = default);
    Task<CatalogItemDetailsDto> CreateAsync(CreateCatalogItemDto input, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateCatalogItemDto input, CancellationToken ct = default);
    Task<CatalogItemDetailsDto> ChangePriceAsync(long id, ChangePriceDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task AssignCategoryAsync(long id, int categoryId, AssignCategoryDto input, CancellationToken ct = default);
    Task RemoveCategoryAsync(long id, int categoryId, CancellationToken ct = default);
}

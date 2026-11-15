using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Dtos.Categories;

namespace Catalog.Bll.Interfaces;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetListAsync(CategoryQuery query, CancellationToken ct = default);
    Task<CategoryDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryWithItemsDto> GetWithItemsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogItemSummaryDto>> GetRecentItemsAsync(
        string slug, DateTime since, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto input, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateCategoryDto input, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

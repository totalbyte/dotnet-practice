using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.Brands;

namespace Catalog.Bll.Interfaces;

public interface IBrandService
{
    Task<PagedResult<BrandDto>> GetListAsync(BrandQuery query, CancellationToken ct = default);
    Task<BrandDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<BrandDto> CreateAsync(CreateBrandDto input, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateBrandDto input, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

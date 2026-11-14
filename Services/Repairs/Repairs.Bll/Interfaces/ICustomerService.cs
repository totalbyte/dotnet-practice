using Repairs.Bll.Dtos.Customers;

namespace Repairs.Bll.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(string? phone, CancellationToken ct = default);
    Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerDto input, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateCustomerDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

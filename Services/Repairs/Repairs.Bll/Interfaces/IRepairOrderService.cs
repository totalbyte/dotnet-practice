using Repairs.Bll.Dtos;
using Repairs.Bll.Dtos.RepairOrders;

namespace Repairs.Bll.Interfaces;

public interface IRepairOrderService
{
    Task<PagedResultDto<RepairOrderSummaryDto>> SearchAsync(RepairOrderQueryDto query, CancellationToken ct = default);
    Task<RepairOrderDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<RepairOrderDto> CreateAsync(CreateRepairOrderDto input, CancellationToken ct = default);
    Task<RepairOrderDto> AddItemAsync(long id, AddRepairOrderItemDto input, CancellationToken ct = default);
    Task ChangeStatusAsync(long id, ChangeRepairOrderStatusDto input, CancellationToken ct = default);
    Task UpdateDetailsAsync(long id, UpdateRepairOrderDetailsDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

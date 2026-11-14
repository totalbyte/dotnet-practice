using Repairs.Domain.Models;

namespace Repairs.Dal.Interfaces;

public interface IRepairOrderRepository : IGenericRepository<RepairOrder>
{
    Task<RepairOrder?> GetWithItemsAsync(long id, CancellationToken ct = default);

    Task<PagedResult<RepairOrderSummary>> SearchAsync(
        string? status, long? customerId, int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<RepairOrderStatusHistory>> GetStatusHistoryAsync(
        long repairOrderId, CancellationToken ct = default);

    Task<bool> HasOpenOrdersAsync(long customerId, CancellationToken ct = default);
    Task AddDetailsAsync(RepairOrderDetails details, CancellationToken ct = default);
    Task AddStatusHistoryAsync(RepairOrderStatusHistory entry, CancellationToken ct = default);
    Task AddItemAsync(RepairOrderItem item, CancellationToken ct = default);

    Task ChangeStatusAsync(
        long repairOrderId, string newStatus, string? changedBy, string? comment, CancellationToken ct = default);

    Task UpdateDetailsAsync(RepairOrderDetails details, CancellationToken ct = default);
}

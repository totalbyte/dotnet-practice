using System.Data;

namespace Repairs.Dal.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    ICustomerRepository Customers { get; }
    IRepairOrderRepository RepairOrders { get; }
    ICatalogItemRepository CatalogItems { get; }

    Task BeginTransactionAsync(
        IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default);

    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

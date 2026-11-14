using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Repairs.Dal.Interfaces;
using Repairs.Domain.Constants;
using Repairs.Domain.Models;

namespace Repairs.Dal.Repositories;

public sealed class RepairOrderRepository : BaseDapperRepository<RepairOrder>, IRepairOrderRepository
{
    public RepairOrderRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(
            connection,
            currentTransaction,
            tableName: "dbo.RepairOrders",
            insertColumns: ["CustomerId", "CreatedBy"],
            activeFilter: "IsDeleted = 0",
            softDeleteAssignment: "IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()")
    {
    }

    public Task<RepairOrder?> GetWithItemsAsync(long id, CancellationToken ct = default)
        => ExecuteDbAsync(async () =>
        {
            const string sql = """
                SELECT o.Id, o.CustomerId, o.Status, o.AcceptedAt, o.IssuedAt,
                       o.CreatedAt, o.CreatedBy, o.UpdatedAt, o.UpdatedBy, o.IsDeleted, o.RowVersion,
                       d.RepairOrderId, d.DeviceType, d.DeviceBrand, d.DeviceModel, d.SerialNumber,
                       d.DefectDescription, d.DiagnosticNotes, d.UpdatedAt, d.UpdatedBy, d.RowVersion,
                       i.RepairOrderId, i.CatalogItemId, i.ItemName, i.UnitPrice, i.Quantity,
                       i.Discount, i.LineTotal, i.AddedAt, i.AddedBy
                FROM dbo.RepairOrders o
                LEFT JOIN dbo.RepairOrderDetails d ON d.RepairOrderId = o.Id
                LEFT JOIN dbo.RepairOrderItems i ON i.RepairOrderId = o.Id
                WHERE o.Id = @Id AND o.IsDeleted = 0
                ORDER BY i.AddedAt, i.CatalogItemId
                """;

            RepairOrder? result = null;

            await Connection.QueryAsync<RepairOrder, RepairOrderDetails?, RepairOrderItem?, RepairOrder>(
                Command(sql, new { Id = id }, ct),
                (order, details, item) =>
                {
                    result ??= order;
                    result.Details ??= details;
                    if (item is not null)
                        result.Items.Add(item);
                    return result;
                },
                splitOn: "RepairOrderId,RepairOrderId");

            return result;
        }, ct);

    public Task<PagedResult<RepairOrderSummary>> SearchAsync(
        string? status, long? customerId, int page, int pageSize, CancellationToken ct = default)
        => ExecuteDbAsync(async () =>
        {
            const string sql = """
                SELECT COUNT(*)
                FROM dbo.vw_RepairOrderSummary
                WHERE (@Status IS NULL OR Status = @Status)
                  AND (@CustomerId IS NULL OR CustomerId = @CustomerId);

                SELECT RepairOrderId, Status, AcceptedAt, IssuedAt, CustomerId, CustomerName, CustomerPhone,
                       DeviceType, DeviceBrand, DeviceModel, SerialNumber, ItemsCount, TotalAmount
                FROM dbo.vw_RepairOrderSummary
                WHERE (@Status IS NULL OR Status = @Status)
                  AND (@CustomerId IS NULL OR CustomerId = @CustomerId)
                ORDER BY AcceptedAt DESC, RepairOrderId DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                """;

            var parameters = new
            {
                Status = status,
                CustomerId = customerId,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            };

            await using var grid = await Connection.QueryMultipleAsync(Command(sql, parameters, ct));
            var totalCount = await grid.ReadSingleAsync<int>();
            var items = (await grid.ReadAsync<RepairOrderSummary>()).AsList();

            return new PagedResult<RepairOrderSummary>(items, totalCount);
        }, ct);

    public Task<IReadOnlyList<RepairOrderStatusHistory>> GetStatusHistoryAsync(
        long repairOrderId, CancellationToken ct = default)
        => ExecuteDbAsync<IReadOnlyList<RepairOrderStatusHistory>>(async () =>
        {
            const string sql = """
                SELECT Id, RepairOrderId, FromStatus, ToStatus, Comment, ChangedAt, ChangedBy
                FROM dbo.RepairOrderStatusHistory
                WHERE RepairOrderId = @RepairOrderId
                ORDER BY ChangedAt, Id
                """;

            var history = await Connection.QueryAsync<RepairOrderStatusHistory>(
                Command(sql, new { RepairOrderId = repairOrderId }, ct));

            return history.AsList();
        }, ct);

    public Task<bool> HasOpenOrdersAsync(long customerId, CancellationToken ct = default)
        => ExecuteDbAsync(() =>
        {
            const string sql = """
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM dbo.RepairOrders
                    WHERE CustomerId = @CustomerId AND IsDeleted = 0 AND Status NOT IN @ClosedStatuses)
                THEN 1 ELSE 0 END
                """;

            var parameters = new { CustomerId = customerId, ClosedStatuses = RepairOrderStatuses.Closed };

            return Connection.ExecuteScalarAsync<bool>(Command(sql, parameters, ct));
        }, ct);

    public Task AddDetailsAsync(RepairOrderDetails details, CancellationToken ct = default)
        => ExecuteDbAsync(() =>
        {
            const string sql = """
                INSERT INTO dbo.RepairOrderDetails
                       (RepairOrderId, DeviceType, DeviceBrand, DeviceModel, SerialNumber, DefectDescription)
                VALUES (@RepairOrderId, @DeviceType, @DeviceBrand, @DeviceModel, @SerialNumber, @DefectDescription)
                """;

            return Connection.ExecuteAsync(Command(sql, details, ct));
        }, ct);

    public Task AddStatusHistoryAsync(RepairOrderStatusHistory entry, CancellationToken ct = default)
        => ExecuteDbAsync(() =>
        {
            const string sql = """
                INSERT INTO dbo.RepairOrderStatusHistory (RepairOrderId, FromStatus, ToStatus, Comment, ChangedBy)
                VALUES (@RepairOrderId, @FromStatus, @ToStatus, @Comment, @ChangedBy)
                """;

            return Connection.ExecuteAsync(Command(sql, entry, ct));
        }, ct);

    public Task AddItemAsync(RepairOrderItem item, CancellationToken ct = default)
        => ExecuteDbAsync(() =>
        {
            const string sql = """
                INSERT INTO dbo.RepairOrderItems
                       (RepairOrderId, CatalogItemId, ItemName, UnitPrice, Quantity, Discount, AddedBy)
                VALUES (@RepairOrderId, @CatalogItemId, @ItemName, @UnitPrice, @Quantity, @Discount, @AddedBy)
                """;

            return Connection.ExecuteAsync(Command(sql, item, ct));
        }, ct);

    public Task ChangeStatusAsync(
        long repairOrderId, string newStatus, string? changedBy, string? comment, CancellationToken ct = default)
        => ExecuteDbAsync(() =>
        {
            var parameters = new
            {
                RepairOrderId = repairOrderId,
                NewStatus = newStatus,
                ChangedBy = changedBy,
                Comment = comment
            };

            return Connection.ExecuteAsync(
                Command("dbo.usp_ChangeRepairOrderStatus", parameters, ct, CommandType.StoredProcedure));
        }, ct);

    public Task UpdateDetailsAsync(RepairOrderDetails details, CancellationToken ct = default)
        => ExecuteDbAsync(() =>
        {
            var parameters = new
            {
                details.RepairOrderId,
                details.DefectDescription,
                details.DiagnosticNotes,
                details.SerialNumber,
                details.RowVersion,
                details.UpdatedBy
            };

            return Connection.ExecuteAsync(
                Command("dbo.usp_UpdateRepairOrderDetails", parameters, ct, CommandType.StoredProcedure));
        }, ct);
}

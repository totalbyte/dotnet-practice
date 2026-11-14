using Dapper;
using Microsoft.Data.SqlClient;
using Repairs.Dal.Interfaces;
using Repairs.Domain.Models;

namespace Repairs.Dal.Repositories;

public sealed class CatalogItemRepository : BaseDapperRepository<CatalogItem>, ICatalogItemRepository
{
    public CatalogItemRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(
            connection,
            currentTransaction,
            tableName: "dbo.CatalogItems",
            insertColumns: ["Id", "Name", "ItemType", "Price", "Stock"],
            activeFilter: "IsActive = 1",
            softDeleteAssignment: "IsActive = 0")
    {
    }

    public Task<bool> DecreaseStockAsync(long id, int quantity, CancellationToken ct = default)
        => ExecuteDbAsync(async () =>
        {
            const string sql = """
                UPDATE dbo.CatalogItems
                SET Stock = Stock - @Quantity
                WHERE Id = @Id AND IsActive = 1 AND Stock >= @Quantity
                """;

            return await Connection.ExecuteAsync(Command(sql, new { Id = id, Quantity = quantity }, ct)) > 0;
        }, ct);

    public Task<bool> SetStockAsync(long id, int stock, CancellationToken ct = default)
        => ExecuteDbAsync(async () =>
        {
            const string sql = """
                UPDATE dbo.CatalogItems
                SET Stock = @Stock
                WHERE Id = @Id AND IsActive = 1
                """;

            return await Connection.ExecuteAsync(Command(sql, new { Id = id, Stock = stock }, ct)) > 0;
        }, ct);
}

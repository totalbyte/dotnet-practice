using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Repairs.Dal.Interfaces;

namespace Repairs.Dal.Repositories;

public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    private readonly string _selectByIdSql;
    private readonly string _selectAllSql;
    private readonly string _insertSql;
    private readonly string _softDeleteSql;

    protected BaseDapperRepository(
        SqlConnection connection,
        Func<SqlTransaction?> currentTransaction,
        string tableName,
        IReadOnlyCollection<string> insertColumns,
        string activeFilter,
        string softDeleteAssignment)
        : base(connection, currentTransaction)
    {
        var columns = string.Join(", ", insertColumns);
        var values = string.Join(", ", insertColumns.Select(column => "@" + column));

        _selectByIdSql = $"SELECT * FROM {tableName} WHERE Id = @Id AND {activeFilter}";
        _selectAllSql = $"SELECT * FROM {tableName} WHERE {activeFilter} ORDER BY Id";
        _insertSql = $"INSERT INTO {tableName} ({columns}) OUTPUT INSERTED.Id VALUES ({values})";
        _softDeleteSql = $"UPDATE {tableName} SET {softDeleteAssignment} WHERE Id = @Id AND {activeFilter}";
    }

    public Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
        => ExecuteDbAsync(
            () => Connection.QuerySingleOrDefaultAsync<T>(Command(_selectByIdSql, new { Id = id }, ct)), ct);

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => ExecuteDbAsync<IReadOnlyList<T>>(
            async () => (await Connection.QueryAsync<T>(Command(_selectAllSql, null, ct))).AsList(), ct);

    public Task<long> AddAsync(T entity, CancellationToken ct = default)
        => ExecuteDbAsync(
            () => Connection.ExecuteScalarAsync<long>(Command(_insertSql, entity, ct)), ct);

    public Task<bool> DeleteAsync(long id, CancellationToken ct = default)
        => ExecuteDbAsync(
            async () => await Connection.ExecuteAsync(Command(_softDeleteSql, new { Id = id }, ct)) > 0, ct);

    protected CommandDefinition Command(
        string sql, object? parameters, CancellationToken ct, CommandType commandType = CommandType.Text)
        => new(sql, parameters, Transaction, CommandTimeoutSeconds, commandType, cancellationToken: ct);
}

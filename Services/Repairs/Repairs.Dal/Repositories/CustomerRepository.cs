using System.Data;
using Microsoft.Data.SqlClient;
using Repairs.Dal.Interfaces;
using Repairs.Domain.Models;

namespace Repairs.Dal.Repositories;

public sealed class CustomerRepository : RepositoryBase, ICustomerRepository
{
    public CustomerRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction)
    {
    }

    public Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default)
        => ExecuteDbAsync(async () =>
        {
            const string sql = """
                SELECT Id, FullName, Email, Phone, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, RowVersion
                FROM dbo.Customers
                WHERE Id = @Id AND IsDeleted = 0
                """;

            await using var command = CreateCommand(sql);
            command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

            await using var reader = await command.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }, ct);

    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
        => ExecuteDbAsync(async () =>
        {
            const string sql = """
                SELECT Id, FullName, Email, Phone, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, RowVersion
                FROM dbo.Customers
                WHERE IsDeleted = 0
                ORDER BY Id
                """;

            await using var command = CreateCommand(sql);
            return await ReadListAsync(command, ct);
        }, ct);

    public Task<IReadOnlyList<Customer>> FindByPhoneAsync(string phone, CancellationToken ct = default)
        => ExecuteDbAsync(async () =>
        {
            const string sql = """
                SELECT Id, FullName, Email, Phone, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, RowVersion
                FROM dbo.Customers
                WHERE Phone = @Phone AND IsDeleted = 0
                ORDER BY Id
                """;

            await using var command = CreateCommand(sql);
            command.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = phone;
            return await ReadListAsync(command, ct);
        }, ct);

    public Task<long> AddAsync(Customer customer, CancellationToken ct = default)
        => ExecuteDbAsync(async () =>
        {
            const string sql = """
                INSERT INTO dbo.Customers (FullName, Email, Phone, CreatedBy)
                OUTPUT INSERTED.Id
                VALUES (@FullName, @Email, @Phone, @CreatedBy)
                """;

            await using var command = CreateCommand(sql);
            command.Parameters.Add("@FullName", SqlDbType.NVarChar, 200).Value = customer.FullName;
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = customer.Email;
            command.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = customer.Phone;
            command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100).Value = ToDbValue(customer.CreatedBy);

            return (long)(await command.ExecuteScalarAsync(ct))!;
        }, ct);

    public Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default)
        => ExecuteDbAsync(async () =>
        {
            const string sql = """
                UPDATE dbo.Customers
                SET FullName = @FullName,
                    Email = @Email,
                    Phone = @Phone,
                    UpdatedAt = SYSUTCDATETIME(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND IsDeleted = 0
                """;

            await using var command = CreateCommand(sql);
            command.Parameters.Add("@Id", SqlDbType.BigInt).Value = customer.Id;
            command.Parameters.Add("@FullName", SqlDbType.NVarChar, 200).Value = customer.FullName;
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = customer.Email;
            command.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = customer.Phone;
            command.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar, 100).Value = ToDbValue(customer.UpdatedBy);

            return await command.ExecuteNonQueryAsync(ct) > 0;
        }, ct);

    public Task<bool> DeleteAsync(long id, CancellationToken ct = default)
        => ExecuteDbAsync(async () =>
        {
            const string sql = """
                UPDATE dbo.Customers
                SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @Id AND IsDeleted = 0
                """;

            await using var command = CreateCommand(sql);
            command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

            return await command.ExecuteNonQueryAsync(ct) > 0;
        }, ct);

    private SqlCommand CreateCommand(string sql)
        => new(sql, Connection, Transaction) { CommandTimeout = CommandTimeoutSeconds };

    private static async Task<IReadOnlyList<Customer>> ReadListAsync(SqlCommand command, CancellationToken ct)
    {
        var customers = new List<Customer>();

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            customers.Add(Map(reader));

        return customers;
    }

    private static Customer Map(SqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        FullName = reader.GetString(1),
        Email = reader.GetString(2),
        Phone = reader.GetString(3),
        CreatedAt = reader.GetDateTime(4),
        CreatedBy = reader.IsDBNull(5) ? null : reader.GetString(5),
        UpdatedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
        UpdatedBy = reader.IsDBNull(7) ? null : reader.GetString(7),
        IsDeleted = reader.GetBoolean(8),
        RowVersion = reader.GetFieldValue<byte[]>(9)
    };

    private static object ToDbValue(string? value) => value is null ? DBNull.Value : value;
}

using System.Data;
using Microsoft.Data.SqlClient;
using Repairs.Dal.Interfaces;
using Repairs.Dal.Repositories;

namespace Repairs.Dal;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;

    public UnitOfWork(string connectionString)
    {
        _connection = new SqlConnection(connectionString);

        Customers = new CustomerRepository(_connection, () => _transaction);
        RepairOrders = new RepairOrderRepository(_connection, () => _transaction);
        CatalogItems = new CatalogItemRepository(_connection, () => _transaction);
    }

    public ICustomerRepository Customers { get; }
    public IRepairOrderRepository RepairOrders { get; }
    public ICatalogItemRepository CatalogItems { get; }

    public async Task BeginTransactionAsync(
        IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress");

        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(ct);

        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(level, ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("There is no active transaction to commit");

        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            return;

        try
        {
            if (_transaction.Connection is not null)
                await _transaction.RollbackAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();

        await _connection.DisposeAsync();
    }
}

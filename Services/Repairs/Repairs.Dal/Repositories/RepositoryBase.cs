using System.Data;
using Microsoft.Data.SqlClient;
using Repairs.Dal.Errors;

namespace Repairs.Dal.Repositories;

public abstract class RepositoryBase
{
    protected const int CommandTimeoutSeconds = 30;

    protected readonly SqlConnection Connection;
    private readonly Func<SqlTransaction?> _currentTransaction;

    protected RepositoryBase(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
    {
        Connection = connection;
        _currentTransaction = currentTransaction;
    }

    protected SqlTransaction? Transaction => _currentTransaction();

    protected async Task EnsureOpenAsync(CancellationToken ct)
    {
        if (Connection.State != ConnectionState.Open)
            await Connection.OpenAsync(ct);
    }

    protected async Task<TResult> ExecuteDbAsync<TResult>(Func<Task<TResult>> operation, CancellationToken ct)
    {
        try
        {
            await EnsureOpenAsync(ct);
            return await operation();
        }
        catch (SqlException ex) when (SqlExceptionTranslator.TryTranslate(ex, out var domainException))
        {
            throw domainException;
        }
    }

    protected async Task ExecuteDbAsync(Func<Task> operation, CancellationToken ct)
    {
        try
        {
            await EnsureOpenAsync(ct);
            await operation();
        }
        catch (SqlException ex) when (SqlExceptionTranslator.TryTranslate(ex, out var domainException))
        {
            throw domainException;
        }
    }
}

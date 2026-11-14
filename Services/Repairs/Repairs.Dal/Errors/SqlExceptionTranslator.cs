using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Repairs.Domain.Exceptions;

namespace Repairs.Dal.Errors;

internal static class SqlExceptionTranslator
{
    public static bool TryTranslate(
        SqlException exception, [NotNullWhen(true)] out DomainException? domainException)
    {
        foreach (SqlError error in exception.Errors)
        {
            domainException = Translate(error, exception);
            if (domainException is not null)
                return true;
        }

        domainException = null;
        return false;
    }

    private static DomainException? Translate(SqlError error, SqlException source) => error.Number switch
    {
        SqlErrorNumbers.RepairOrderNotFound
            or SqlErrorNumbers.CustomerNotFound
            or SqlErrorNumbers.CatalogItemNotFound
            => new NotFoundException(error.Message, source),

        SqlErrorNumbers.StatusTransitionNotAllowed
            or SqlErrorNumbers.OrderHasNoItems
            or SqlErrorNumbers.ItemsLocked
            or SqlErrorNumbers.InsufficientStock
            or SqlErrorNumbers.ConcurrencyConflict
            or SqlErrorNumbers.OrderNotDeletable
            => new BusinessConflictException(error.Message, source),

        SqlErrorNumbers.InvalidArgument
            => new ValidationException(error.Message, source),

        SqlErrorNumbers.UniqueIndexViolation
            or SqlErrorNumbers.UniqueConstraintViolation
            => new BusinessConflictException("Record with the same key already exists", source),

        SqlErrorNumbers.ConstraintViolation
            => new BusinessConflictException("Operation violates a data integrity constraint", source),

        _ => null
    };
}

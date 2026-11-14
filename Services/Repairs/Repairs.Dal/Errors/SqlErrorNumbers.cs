namespace Repairs.Dal.Errors;

internal static class SqlErrorNumbers
{
    public const int RepairOrderNotFound = 50001;
    public const int StatusTransitionNotAllowed = 50002;
    public const int OrderHasNoItems = 50003;
    public const int CustomerNotFound = 50004;
    public const int ItemsLocked = 50005;
    public const int CatalogItemNotFound = 50006;
    public const int InsufficientStock = 50007;
    public const int ConcurrencyConflict = 50008;
    public const int OrderNotDeletable = 50009;
    public const int InvalidArgument = 50010;

    public const int ConstraintViolation = 547;
    public const int UniqueIndexViolation = 2601;
    public const int UniqueConstraintViolation = 2627;
}

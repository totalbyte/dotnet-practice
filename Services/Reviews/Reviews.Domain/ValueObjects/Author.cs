using Reviews.Domain.Common;

namespace Reviews.Domain.ValueObjects;

public sealed class Author : ValueObject
{
    public const int MaxDisplayNameLength = 200;

    public Author(long customerId, string displayName)
    {
        CustomerId = Guard.Positive(customerId, "CustomerId");
        DisplayName = Guard.Required(displayName, "DisplayName", MaxDisplayNameLength);
    }

    public long CustomerId { get; }

    public string DisplayName { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CustomerId;
        yield return DisplayName;
    }
}

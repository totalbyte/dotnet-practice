using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Tests;

public sealed class ValueObjectTests
{
    [Fact]
    public void Value_objects_with_same_components_are_equal()
    {
        var first = new Author(1, "Olena");
        var second = new Author(1, " Olena ");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Value_objects_with_different_components_are_not_equal()
    {
        Assert.NotEqual(new Device("TV", "LG", "43UP7500"), new Device("TV", null, "43UP7500"));
        Assert.True(new Author(1, "Olena") != new Author(2, "Olena"));
    }

    [Theory]
    [InlineData(0, "Olena")]
    [InlineData(1, "")]
    [InlineData(1, "   ")]
    public void Author_is_validated_in_constructor(long customerId, string displayName)
        => Assert.Throws<DomainException>(() => new Author(customerId, displayName));

    [Fact]
    public void Device_requires_type_and_model()
    {
        Assert.Throws<DomainException>(() => new Device("", "LG", "43UP7500"));
        Assert.Throws<DomainException>(() => new Device("TV", "LG", " "));
    }
}

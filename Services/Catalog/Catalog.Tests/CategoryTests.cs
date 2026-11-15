using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Tests;

public class CategoryTests
{
    [Theory]
    [InlineData("")]
    [InlineData("two words")]
    public void Constructor_InvalidSlug_Throws(string slug)
    {
        Assert.Throws<DomainException>(() => new Category("Cables", slug));
    }

    [Fact]
    public void ChangeDisplayOrder_Negative_Throws()
    {
        var category = new Category("Cables", "cables");

        Assert.Throws<DomainException>(() => category.ChangeDisplayOrder(-1));
    }

    [Fact]
    public void MoveTo_OwnId_Throws()
    {
        var category = new Category("Cables", "cables").WithId(5);

        Assert.Throws<DomainException>(() => category.MoveTo(5));
    }

    [Fact]
    public void MoveTo_OtherCategory_ChangesParent()
    {
        var category = new Category("Cables", "cables").WithId(5);

        category.MoveTo(1);

        Assert.Equal(1, category.ParentCategoryId);
    }
}

using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Tests;

public class CatalogItemTests
{
    private static CatalogItem Part(decimal price = 100m, int? brandId = 1)
        => new("PRT-TEST", "Test part", CatalogItemType.Part, price, brandId);

    private static CatalogItem Service()
        => new("SRV-TEST", "Test service", CatalogItemType.Service, 300m);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_PriceIsNotPositive_Throws(decimal price)
    {
        Assert.Throws<DomainException>(() => Part(price));
    }

    [Fact]
    public void Constructor_ServiceWithBrand_Throws()
    {
        Assert.Throws<DomainException>(
            () => new CatalogItem("SRV-TEST", "Test service", CatalogItemType.Service, 300m, brandId: 1));
    }

    [Fact]
    public void Constructor_ValidPart_IsActiveAndNotDeleted()
    {
        var item = Part();

        Assert.True(item.IsActive);
        Assert.False(item.IsDeleted);
        Assert.Equal(1, item.BrandId);
    }

    [Fact]
    public void ChangePrice_PersistedItem_UpdatesPriceAndAuditFields()
    {
        var item = Part().WithId(7L);

        item.ChangePrice(149.999m, "manager");

        Assert.Equal(150.00m, item.Price);
        Assert.NotNull(item.UpdatedAt);
        Assert.Equal("manager", item.UpdatedBy);
    }

    [Fact]
    public void ChangePrice_NotPositive_ThrowsAndKeepsOldPrice()
    {
        var item = Part(100m);

        Assert.Throws<DomainException>(() => item.ChangePrice(0));
        Assert.Equal(100m, item.Price);
    }

    [Fact]
    public void AssignBrand_Service_Throws()
    {
        Assert.Throws<DomainException>(() => Service().AssignBrand(2));
    }

    [Fact]
    public void Describe_DurationForPart_Throws()
    {
        Assert.Throws<DomainException>(() => Part().Describe("Text", 6, 30, null));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    public void Describe_SpecificationsIsNotJsonObject_Throws(string specifications)
    {
        Assert.Throws<DomainException>(() => Part().Describe(null, 0, null, specifications));
    }

    [Fact]
    public void Describe_CalledTwice_KeepsSingleDetail()
    {
        var item = Service();

        item.Describe("First", 1, 60, null);
        item.Describe("Second", 3, 90, null);

        Assert.Equal("Second", item.Detail!.Description);
        Assert.Equal(3, item.Detail.WarrantyMonths);
        Assert.Equal(90, item.Detail.EstimatedDurationMinutes);
    }

    [Fact]
    public void AssignCategory_SecondPrimary_Throws()
    {
        var item = Part();
        item.AssignCategory(3, isPrimary: true);

        Assert.Throws<DomainException>(() => item.AssignCategory(7, isPrimary: true));
        Assert.Single(item.Categories);
    }

    [Fact]
    public void AssignCategory_SameCategoryAgain_ChangesPrimaryFlagOnly()
    {
        var item = Part();
        item.AssignCategory(3);

        item.AssignCategory(3, isPrimary: true);

        var link = Assert.Single(item.Categories);
        Assert.True(link.IsPrimary);
    }

    [Fact]
    public void RemoveFromCategory_NotAssigned_Throws()
    {
        Assert.Throws<DomainException>(() => Part().RemoveFromCategory(3));
    }

    [Fact]
    public void MarkDeleted_DeactivatesItem_AndBlocksActivation()
    {
        var item = Part();

        item.MarkDeleted();

        Assert.True(item.IsDeleted);
        Assert.False(item.IsActive);
        Assert.Throws<DomainException>(() => item.Activate());
    }
}

using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Dtos.Categories;
using Catalog.Bll.Validation;
using Catalog.Domain.Entities;

namespace Catalog.Tests;

public class ValidatorTests
{
    private static CreateCatalogItemDto ValidPart() => new()
    {
        Sku = "PRT-CBL-USBC",
        Name = "USB-C cable",
        ItemType = CatalogItemType.Part,
        Price = 199.90m,
        BrandId = 2,
        Details = new CatalogItemDetailInputDto { WarrantyMonths = 12, Specifications = """{"lengthM":1}""" }
    };

    private static string[] InvalidProperties(CreateCatalogItemDto input)
        => new CreateCatalogItemDtoValidator().Validate(input).Errors.Select(e => e.PropertyName).ToArray();

    [Fact]
    public void CreateItem_ValidPart_Passes()
    {
        Assert.Empty(InvalidProperties(ValidPart()));
    }

    [Theory]
    [InlineData("prt-lower")]
    [InlineData("PRT CBL")]
    [InlineData("PRT--CBL")]
    public void CreateItem_SkuFormatIsInvalid_Fails(string sku)
    {
        Assert.Contains(nameof(CreateCatalogItemDto.Sku), InvalidProperties(ValidPart() with { Sku = sku }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.999)]
    public void CreateItem_PriceIsInvalid_Fails(decimal price)
    {
        Assert.Contains(nameof(CreateCatalogItemDto.Price), InvalidProperties(ValidPart() with { Price = price }));
    }

    [Fact]
    public void CreateItem_ServiceWithBrand_Fails()
    {
        var input = ValidPart() with { ItemType = CatalogItemType.Service };

        Assert.Contains(nameof(CreateCatalogItemDto.BrandId), InvalidProperties(input));
    }

    [Fact]
    public void CreateItem_PartWithDurationAndBrokenSpecifications_FailsForBothFields()
    {
        var input = ValidPart() with
        {
            Details = new CatalogItemDetailInputDto { EstimatedDurationMinutes = 30, Specifications = "{oops" }
        };

        var properties = InvalidProperties(input);

        Assert.Contains("Details.EstimatedDurationMinutes", properties);
        Assert.Contains("Details.Specifications", properties);
    }

    [Fact]
    public void ItemQuery_MaxPriceBelowMinPriceAndUnknownSort_Fails()
    {
        var query = new CatalogItemQuery { MinPrice = 500m, MaxPrice = 100m, SortBy = "weight", SortDir = "up" };

        var properties = new CatalogItemQueryValidator().Validate(query).Errors.Select(e => e.PropertyName);

        Assert.Equal(
            [nameof(CatalogItemQuery.MaxPrice), nameof(CatalogItemQuery.SortBy), nameof(CatalogItemQuery.SortDir)],
            properties);
    }

    [Fact]
    public void ItemQuery_Defaults_Pass()
    {
        Assert.True(new CatalogItemQueryValidator().Validate(new CatalogItemQuery()).IsValid);
    }

    [Theory]
    [InlineData("cables", true)]
    [InlineData("usb-c-cables", true)]
    [InlineData("Cables", false)]
    [InlineData("usb--c", false)]
    public void CreateCategory_SlugFormat_IsChecked(string slug, bool expected)
    {
        var result = new CreateCategoryDtoValidator().Validate(new CreateCategoryDto { Name = "Cables", Slug = slug });

        Assert.Equal(expected, result.IsValid);
    }
}

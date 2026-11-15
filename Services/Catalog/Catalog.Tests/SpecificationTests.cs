using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Specifications;
using Catalog.Domain.Entities;

namespace Catalog.Tests;

public class SpecificationTests
{
    private static readonly CatalogItem[] Items = CreateItems();

    private static CatalogItem[] CreateItems()
    {
        var display = new CatalogItem("PRT-DSP", "Display", CatalogItemType.Part, 4200m, brandId: 1).WithId(1L);
        display.AssignCategory(3, isPrimary: true);
        var battery = new CatalogItem("PRT-BAT", "Battery", CatalogItemType.Part, 1450m, brandId: 1).WithId(2L);
        battery.AssignCategory(4, isPrimary: true);
        var paste = new CatalogItem("PRT-TP", "Thermal paste", CatalogItemType.Part, 320m, brandId: 4).WithId(3L);
        var diagnostics = new CatalogItem("SRV-DIAG", "Diagnostics", CatalogItemType.Service, 300m).WithId(4L);
        var cleaning = new CatalogItem("SRV-CLEAN", "Cleaning", CatalogItemType.Service, 650m).WithId(5L);
        cleaning.Deactivate();

        return [display, battery, paste, diagnostics, cleaning];
    }

    private static string[] Skus(CatalogItemQuery query, int pageSize = 20)
        => new CatalogItemsFilterSpec(query, pageSize).Evaluate(Items).Select(i => i.Sku).ToArray();

    [Fact]
    public void FilterSpec_TypeAndPriceRange_ReturnsMatchingItemsOnly()
    {
        var skus = Skus(new CatalogItemQuery { ItemType = CatalogItemType.Part, MinPrice = 400m, MaxPrice = 2000m });

        Assert.Equal(["PRT-BAT"], skus);
    }

    [Fact]
    public void FilterSpec_SearchMatchesNameOrSku()
    {
        Assert.Equal(["SRV-DIAG"], Skus(new CatalogItemQuery { Search = "Diag" }));
        Assert.Equal(["SRV-CLEAN", "SRV-DIAG"], Skus(new CatalogItemQuery { Search = "SRV" }));
    }

    [Fact]
    public void FilterSpec_CategoryBrandAndActivity_AreApplied()
    {
        Assert.Equal(["PRT-DSP"], Skus(new CatalogItemQuery { CategoryId = 3 }));
        Assert.Equal(["PRT-BAT", "PRT-DSP"], Skus(new CatalogItemQuery { BrandId = 1 }));
        Assert.Equal(["SRV-CLEAN"], Skus(new CatalogItemQuery { IsActive = false }));
    }

    [Fact]
    public void FilterSpec_SortByPriceDescending_OrdersItems()
    {
        var skus = Skus(new CatalogItemQuery { SortBy = "price", SortDir = "desc" });

        Assert.Equal(["PRT-DSP", "PRT-BAT", "SRV-CLEAN", "PRT-TP", "SRV-DIAG"], skus);
    }

    [Fact]
    public void FilterSpec_UnknownSortField_FallsBackToName()
    {
        var skus = Skus(new CatalogItemQuery { SortBy = "rowversion" });

        Assert.Equal(["PRT-BAT", "SRV-CLEAN", "SRV-DIAG", "PRT-DSP", "PRT-TP"], skus);
    }

    [Fact]
    public void FilterSpec_SecondPage_SkipsFirstPage()
    {
        var skus = Skus(new CatalogItemQuery { Page = 2, SortBy = "price" }, pageSize: 2);

        Assert.Equal(["SRV-CLEAN", "PRT-BAT"], skus);
    }

    [Fact]
    public void FilterSpec_Criteria_DoNotDependOnPaging()
    {
        var specification = new CatalogItemsFilterSpec(
            new CatalogItemQuery { ItemType = CatalogItemType.Part, Page = 2 }, pageSize: 1);

        Assert.Equal(3, Items.Count(specification.IsSatisfiedBy));
        Assert.Single(specification.Evaluate(Items));
    }

    [Fact]
    public void BrandByNameSpec_ExcludesGivenId()
    {
        Brand[] brands = [new Brand("Apple").WithId(1), new Brand("Samsung").WithId(2)];

        Assert.Single(new BrandByNameSpec("Apple").Evaluate(brands));
        Assert.Empty(new BrandByNameSpec("Apple", exceptId: 1).Evaluate(brands));
    }
}

using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Domain.Entities;

namespace Catalog.Tests;

public class MappingProfileTests
{
    [Fact]
    public void Configuration_IsValid()
    {
        TestSupport.CreateMapperConfiguration().AssertConfigurationIsValid();
    }

    [Fact]
    public void CatalogItem_WithDetail_MapsToDetailsDto()
    {
        var item = new CatalogItem("SRV-DIAG", "Diagnostics", CatalogItemType.Service, 300m).WithId(6L);
        item.Describe("Full diagnostics", 0, 60, null);

        var dto = TestSupport.CreateMapper().Map<CatalogItemDetailsDto>(item);

        Assert.Equal(6, dto.Id);
        Assert.Equal("Service", dto.ItemType);
        Assert.Equal("Full diagnostics", dto.Description);
        Assert.Equal(60, dto.EstimatedDurationMinutes);
        Assert.Null(dto.BrandName);
        Assert.Empty(dto.Categories);
    }

    [Fact]
    public void CatalogItem_WithoutDetail_MapsToDefaults()
    {
        var item = new CatalogItem("PRT-TP", "Thermal paste", CatalogItemType.Part, 320m, brandId: 4);

        var dto = TestSupport.CreateMapper().Map<CatalogItemDetailsDto>(item);

        Assert.Null(dto.Description);
        Assert.Equal(0, dto.WarrantyMonths);
        Assert.Equal(4, dto.BrandId);
    }
}

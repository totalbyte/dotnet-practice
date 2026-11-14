using Repairs.Bll.Dtos.RepairOrders;
using Repairs.Domain.Models;

namespace Repairs.Tests;

public class MappingProfileTests
{
    [Fact]
    public void Configuration_IsValid()
    {
        TestMapper.CreateConfiguration().AssertConfigurationIsValid();
    }

    [Fact]
    public void RepairOrder_MapsDeviceItemsAndTotal()
    {
        var order = new RepairOrder
        {
            Id = 7,
            Details = new RepairOrderDetails { DeviceModel = "iPhone 13", RowVersion = [0, 0, 0, 0, 0, 0, 7, 209] },
            Items =
            [
                new RepairOrderItem { CatalogItemId = 1, LineTotal = 4200m },
                new RepairOrderItem { CatalogItemId = 7, LineTotal = 800m }
            ]
        };

        var dto = TestMapper.Create().Map<RepairOrderDto>(order);

        Assert.Equal("iPhone 13", dto.Device?.DeviceModel);
        Assert.Equal("AAAAAAAAB9E=", dto.Device?.RowVersion);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal(5000m, dto.TotalAmount);
    }
}

using System.ComponentModel.DataAnnotations;
using Repairs.Domain.Constants;

namespace Repairs.Bll.Dtos.RepairOrders;

public sealed class CreateRepairOrderDto
{
    [Range(1, long.MaxValue)]
    public long CustomerId { get; set; }

    [Required]
    [AllowedValues(
        DeviceTypes.Smartphone, DeviceTypes.Laptop, DeviceTypes.Tablet, DeviceTypes.Desktop,
        DeviceTypes.Tv, DeviceTypes.Appliance, DeviceTypes.Other)]
    public string DeviceType { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DeviceBrand { get; set; }

    [Required, StringLength(100)]
    public string DeviceModel { get; set; } = string.Empty;

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [Required, StringLength(1000, MinimumLength = 5)]
    public string DefectDescription { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Required, MaxLength(50)]
    public List<AddRepairOrderItemDto> Items { get; set; } = [];
}

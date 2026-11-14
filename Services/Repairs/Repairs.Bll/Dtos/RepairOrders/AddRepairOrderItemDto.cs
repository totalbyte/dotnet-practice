using System.ComponentModel.DataAnnotations;

namespace Repairs.Bll.Dtos.RepairOrders;

public sealed class AddRepairOrderItemDto
{
    [Range(1, long.MaxValue)]
    public long CatalogItemId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal Discount { get; set; }

    [StringLength(100)]
    public string? AddedBy { get; set; }
}

namespace Repairs.Domain.Models;

public class RepairOrderItem
{
    public long RepairOrderId { get; set; }
    public long CatalogItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime AddedAt { get; set; }
    public string? AddedBy { get; set; }
}

namespace Repairs.Bll.Dtos.RepairOrders;

public sealed record RepairOrderItemDto
{
    public long CatalogItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Discount { get; init; }
    public decimal LineTotal { get; init; }
    public DateTime AddedAt { get; init; }
}

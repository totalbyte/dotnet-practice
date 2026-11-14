namespace Repairs.Bll.Dtos.RepairOrders;

public sealed record RepairOrderSummaryDto
{
    public long RepairOrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime AcceptedAt { get; init; }
    public DateTime? IssuedAt { get; init; }
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? DeviceType { get; init; }
    public string? DeviceBrand { get; init; }
    public string? DeviceModel { get; init; }
    public string? SerialNumber { get; init; }
    public int ItemsCount { get; init; }
    public decimal TotalAmount { get; init; }
}

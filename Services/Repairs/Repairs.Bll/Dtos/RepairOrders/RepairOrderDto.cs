namespace Repairs.Bll.Dtos.RepairOrders;

public sealed record RepairOrderDto
{
    public long Id { get; init; }
    public long CustomerId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime AcceptedAt { get; init; }
    public DateTime? IssuedAt { get; init; }
    public DeviceDto? Device { get; init; }
    public IReadOnlyList<RepairOrderItemDto> Items { get; init; } = [];
    public IReadOnlyList<StatusHistoryDto> History { get; init; } = [];
    public decimal TotalAmount { get; init; }
}

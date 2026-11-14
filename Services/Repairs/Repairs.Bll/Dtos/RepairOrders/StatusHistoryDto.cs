namespace Repairs.Bll.Dtos.RepairOrders;

public sealed record StatusHistoryDto
{
    public string? FromStatus { get; init; }
    public string ToStatus { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public DateTime ChangedAt { get; init; }
    public string? ChangedBy { get; init; }
}

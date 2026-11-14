namespace Repairs.Domain.Models;

public class RepairOrderStatusHistory
{
    public long Id { get; set; }
    public long RepairOrderId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
}

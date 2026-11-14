namespace Repairs.Domain.Models;

public class RepairOrderSummary
{
    public long RepairOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
    public string? DeviceBrand { get; set; }
    public string? DeviceModel { get; set; }
    public string? SerialNumber { get; set; }
    public int ItemsCount { get; set; }
    public decimal TotalAmount { get; set; }
}

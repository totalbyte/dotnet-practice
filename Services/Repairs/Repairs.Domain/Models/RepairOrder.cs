using Repairs.Domain.Constants;

namespace Repairs.Domain.Models;

public class RepairOrder
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Status { get; set; } = RepairOrderStatuses.Accepted;
    public DateTime AcceptedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public RepairOrderDetails? Details { get; set; }
    public List<RepairOrderItem> Items { get; set; } = [];
    public List<RepairOrderStatusHistory> History { get; set; } = [];
}

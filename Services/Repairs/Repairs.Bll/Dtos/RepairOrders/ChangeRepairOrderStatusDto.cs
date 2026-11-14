using System.ComponentModel.DataAnnotations;
using Repairs.Domain.Constants;

namespace Repairs.Bll.Dtos.RepairOrders;

public sealed class ChangeRepairOrderStatusDto
{
    [Required]
    [AllowedValues(
        RepairOrderStatuses.Diagnosed, RepairOrderStatuses.Approved, RepairOrderStatuses.InRepair,
        RepairOrderStatuses.Ready, RepairOrderStatuses.Paid, RepairOrderStatuses.Issued,
        RepairOrderStatuses.Cancelled)]
    public string Status { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Comment { get; set; }

    [StringLength(100)]
    public string? ChangedBy { get; set; }
}

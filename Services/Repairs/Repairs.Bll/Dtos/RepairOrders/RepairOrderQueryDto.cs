using System.ComponentModel.DataAnnotations;
using Repairs.Domain.Constants;

namespace Repairs.Bll.Dtos.RepairOrders;

public sealed class RepairOrderQueryDto
{
    [AllowedValues(
        null,
        RepairOrderStatuses.Accepted, RepairOrderStatuses.Diagnosed, RepairOrderStatuses.Approved,
        RepairOrderStatuses.InRepair, RepairOrderStatuses.Ready, RepairOrderStatuses.Paid,
        RepairOrderStatuses.Issued, RepairOrderStatuses.Cancelled)]
    public string? Status { get; set; }

    [Range(1, long.MaxValue)]
    public long? CustomerId { get; set; }

    [Range(1, 100_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

using System.ComponentModel.DataAnnotations;

namespace Repairs.Bll.Dtos.RepairOrders;

public sealed class UpdateRepairOrderDetailsDto
{
    [Required, StringLength(1000, MinimumLength = 5)]
    public string DefectDescription { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? DiagnosticNotes { get; set; }

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [StringLength(100)]
    public string? UpdatedBy { get; set; }
}

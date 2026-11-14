namespace Repairs.Domain.Models;

public class RepairOrderDetails
{
    public long RepairOrderId { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string? DeviceBrand { get; set; }
    public string DeviceModel { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string DefectDescription { get; set; } = string.Empty;
    public string? DiagnosticNotes { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

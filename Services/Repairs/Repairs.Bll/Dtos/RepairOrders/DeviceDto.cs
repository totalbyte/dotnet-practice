namespace Repairs.Bll.Dtos.RepairOrders;

public sealed record DeviceDto
{
    public string DeviceType { get; init; } = string.Empty;
    public string? DeviceBrand { get; init; }
    public string DeviceModel { get; init; } = string.Empty;
    public string? SerialNumber { get; init; }
    public string DefectDescription { get; init; } = string.Empty;
    public string? DiagnosticNotes { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

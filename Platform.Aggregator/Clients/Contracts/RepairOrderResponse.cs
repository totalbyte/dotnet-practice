namespace Platform.Aggregator.Clients.Contracts;

public sealed record RepairOrderResponse
{
    public long Id { get; init; }
    public long CustomerId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime AcceptedAt { get; init; }
    public DateTime? IssuedAt { get; init; }
    public RepairOrderDeviceResponse? Device { get; init; }
    public IReadOnlyList<RepairOrderItemResponse> Items { get; init; } = [];
    public decimal TotalAmount { get; init; }
}

public sealed record RepairOrderDeviceResponse
{
    public string DeviceType { get; init; } = string.Empty;
    public string? DeviceBrand { get; init; }
    public string DeviceModel { get; init; } = string.Empty;
    public string DefectDescription { get; init; } = string.Empty;
    public string? DiagnosticNotes { get; init; }
}

public sealed record RepairOrderItemResponse
{
    public long CatalogItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Discount { get; init; }
    public decimal LineTotal { get; init; }
}

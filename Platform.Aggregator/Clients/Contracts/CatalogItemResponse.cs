namespace Platform.Aggregator.Clients.Contracts;

public sealed record CatalogItemResponse
{
    public long Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string? BrandName { get; init; }
    public bool IsActive { get; init; }
    public int WarrantyMonths { get; init; }
}

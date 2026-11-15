namespace Catalog.Bll.Dtos.CatalogItems;

public sealed record CatalogItemDetailsDto
{
    public long Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int? BrandId { get; init; }
    public string? BrandName { get; init; }
    public bool IsActive { get; init; }
    public string? Description { get; init; }
    public int WarrantyMonths { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public string? Specifications { get; init; }
    public IReadOnlyList<ItemCategoryDto> Categories { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

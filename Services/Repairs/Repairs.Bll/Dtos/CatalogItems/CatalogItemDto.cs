namespace Repairs.Bll.Dtos.CatalogItems;

public sealed record CatalogItemDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
}

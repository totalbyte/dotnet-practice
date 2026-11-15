namespace Catalog.Bll.Dtos.CatalogItems;

public sealed record CatalogItemDto(
    long Id, string Sku, string Name, string ItemType, decimal Price,
    int? BrandId, string? BrandName, bool IsActive);

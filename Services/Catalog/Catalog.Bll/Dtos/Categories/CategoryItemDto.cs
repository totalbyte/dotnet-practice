namespace Catalog.Bll.Dtos.Categories;

public sealed record CategoryItemDto(
    long CatalogItemId, string Sku, string Name, decimal Price, DateTime AddedAt, bool IsPrimary);

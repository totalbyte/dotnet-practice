namespace Catalog.Bll.Dtos.CatalogItems;

public sealed record ItemCategoryDto(int CategoryId, string Name, string Slug, DateTime AddedAt, bool IsPrimary);

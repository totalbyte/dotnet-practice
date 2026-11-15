namespace Catalog.Bll.Dtos.CatalogItems;

public sealed record CatalogItemSummaryDto(long Id, string Sku, string Name, string ItemType, decimal Price);

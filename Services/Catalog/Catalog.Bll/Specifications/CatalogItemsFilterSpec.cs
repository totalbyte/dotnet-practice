using Ardalis.Specification;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class CatalogItemsFilterSpec : Specification<CatalogItem>
{
    public CatalogItemsFilterSpec(CatalogItemQuery query, int pageSize)
    {
        Query.AsNoTracking();
        Query.Include(i => i.Brand);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            Query.Where(i => i.Name.Contains(search) || i.Sku.Contains(search));
        }

        if (query.ItemType is { } itemType)
            Query.Where(i => i.ItemType == itemType);
        if (query.BrandId is { } brandId)
            Query.Where(i => i.BrandId == brandId);
        if (query.CategoryId is { } categoryId)
            Query.Where(i => i.Categories.Any(c => c.CategoryId == categoryId));
        if (query.MinPrice is { } minPrice)
            Query.Where(i => i.Price >= minPrice);
        if (query.MaxPrice is { } maxPrice)
            Query.Where(i => i.Price <= maxPrice);
        if (query.IsActive is { } isActive)
            Query.Where(i => i.IsActive == isActive);

        var ordered = (query.SortBy.ToLowerInvariant(), query.IsDescending()) switch
        {
            (CatalogItemQuery.SortByPrice, true) => Query.OrderByDescending(i => i.Price),
            (CatalogItemQuery.SortByPrice, false) => Query.OrderBy(i => i.Price),
            (CatalogItemQuery.SortBySku, true) => Query.OrderByDescending(i => i.Sku),
            (CatalogItemQuery.SortBySku, false) => Query.OrderBy(i => i.Sku),
            (CatalogItemQuery.SortByCreatedAt, true) => Query.OrderByDescending(i => i.CreatedAt),
            (CatalogItemQuery.SortByCreatedAt, false) => Query.OrderBy(i => i.CreatedAt),
            (_, true) => Query.OrderByDescending(i => i.Name),
            _ => Query.OrderBy(i => i.Name)
        };
        ordered.ThenBy(i => i.Id);

        Query.Skip((query.Page - 1) * pageSize).Take(pageSize);
    }
}

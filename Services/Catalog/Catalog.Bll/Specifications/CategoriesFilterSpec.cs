using Ardalis.Specification;
using Catalog.Bll.Dtos.Categories;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class CategoriesFilterSpec : Specification<Category>
{
    public CategoriesFilterSpec(CategoryQuery query, int pageSize)
    {
        Query.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            Query.Where(c => c.Name.Contains(search) || c.Slug.Contains(search));
        }

        if (query.ParentCategoryId is { } parentId)
            Query.Where(c => c.ParentCategoryId == parentId);

        var ordered = (query.SortBy.ToLowerInvariant(), query.IsDescending()) switch
        {
            (CategoryQuery.SortByDisplayOrder, true) => Query.OrderByDescending(c => c.DisplayOrder),
            (CategoryQuery.SortByDisplayOrder, false) => Query.OrderBy(c => c.DisplayOrder),
            (_, true) => Query.OrderByDescending(c => c.Name),
            _ => Query.OrderBy(c => c.Name)
        };
        ordered.ThenBy(c => c.Id);

        Query.Skip((query.Page - 1) * pageSize).Take(pageSize);
    }
}

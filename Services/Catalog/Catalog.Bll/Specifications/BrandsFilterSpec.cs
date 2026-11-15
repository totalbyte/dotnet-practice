using Ardalis.Specification;
using Catalog.Bll.Dtos.Brands;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class BrandsFilterSpec : Specification<Brand>
{
    public BrandsFilterSpec(BrandQuery query, int pageSize)
    {
        Query.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            Query.Where(b => b.Name.Contains(search));
        }

        if (query.IsDescending())
            Query.OrderByDescending(b => b.Name);
        else
            Query.OrderBy(b => b.Name);

        Query.Skip((query.Page - 1) * pageSize).Take(pageSize);
    }
}

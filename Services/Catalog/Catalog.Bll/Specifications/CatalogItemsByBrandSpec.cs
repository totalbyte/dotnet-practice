using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class CatalogItemsByBrandSpec : Specification<CatalogItem>
{
    public CatalogItemsByBrandSpec(int brandId)
    {
        Query.IgnoreQueryFilters();
        Query.Where(i => i.BrandId == brandId);
    }
}

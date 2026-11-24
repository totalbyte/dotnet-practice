using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class CatalogItemsByIdsSpec : Specification<CatalogItem>
{
    public CatalogItemsByIdsSpec(IReadOnlyCollection<long> ids)
    {
        Query.AsNoTracking();
        Query.AsSplitQuery();
        Query.Include(i => i.Brand);
        Query.Include(i => i.Detail);
        Query.Include(i => i.Categories).ThenInclude(ic => ic.Category);
        Query.Where(i => ids.Contains(i.Id));
    }
}

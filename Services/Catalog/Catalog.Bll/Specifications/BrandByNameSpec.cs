using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class BrandByNameSpec : Specification<Brand>
{
    public BrandByNameSpec(string name, int? exceptId = null)
    {
        Query.Where(b => b.Name == name);
        if (exceptId is { } id)
            Query.Where(b => b.Id != id);
    }
}

using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class CategoryBySlugSpec : Specification<Category>
{
    public CategoryBySlugSpec(string slug)
    {
        Query.Where(c => c.Slug == slug);
    }
}

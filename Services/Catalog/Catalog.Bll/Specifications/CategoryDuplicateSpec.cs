using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class CategoryDuplicateSpec : Specification<Category>
{
    public CategoryDuplicateSpec(string name, string slug, int? exceptId = null)
    {
        Query.Where(c => c.Name == name || c.Slug == slug);
        if (exceptId is { } id)
            Query.Where(c => c.Id != id);
    }
}

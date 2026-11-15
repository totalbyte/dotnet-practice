using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class CategoryChildrenSpec : Specification<Category>
{
    public CategoryChildrenSpec(int parentCategoryId)
    {
        Query.Where(c => c.ParentCategoryId == parentCategoryId);
    }
}

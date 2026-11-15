using Catalog.Bll.Dtos.Categories;
using FluentValidation;

namespace Catalog.Bll.Validation;

public sealed class CategoryQueryValidator : AbstractValidator<CategoryQuery>
{
    public CategoryQueryValidator()
    {
        RuleFor(x => x.SortBy).SortField(CategoryQuery.SortFields);
        RuleFor(x => x.SortDir).SortDirection();
    }
}

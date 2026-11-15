using Catalog.Bll.Dtos.Brands;
using FluentValidation;

namespace Catalog.Bll.Validation;

public sealed class BrandQueryValidator : AbstractValidator<BrandQuery>
{
    public BrandQueryValidator()
    {
        RuleFor(x => x.SortDir).SortDirection();
    }
}

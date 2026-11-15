using Catalog.Bll.Dtos.CatalogItems;
using FluentValidation;

namespace Catalog.Bll.Validation;

public sealed class CatalogItemQueryValidator : AbstractValidator<CatalogItemQuery>
{
    public CatalogItemQueryValidator()
    {
        RuleFor(x => x.ItemType).IsInEnum();
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice is not null && x.MaxPrice is not null)
            .WithMessage("'Max Price' must not be less than 'Min Price'");
        RuleFor(x => x.SortBy).SortField(CatalogItemQuery.SortFields);
        RuleFor(x => x.SortDir).SortDirection();
    }
}

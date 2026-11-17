using FluentValidation;
using Reviews.Application.Common.Validation;

namespace Reviews.Application.Reviews.Queries.GetReviewsFeed;

public sealed class GetReviewsFeedQueryValidator : AbstractValidator<GetReviewsFeedQuery>
{
    public GetReviewsFeedQueryValidator()
    {
        RuleFor(x => x.CatalogItemId).GreaterThan(0).When(x => x.CatalogItemId is not null);
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId is not null);
        RuleFor(x => x.After).MustBeObjectId().When(x => x.After is not null);
        RuleFor(x => x.Limit).InclusiveBetween(1, ValidationRules.MaxPageSize);
    }
}

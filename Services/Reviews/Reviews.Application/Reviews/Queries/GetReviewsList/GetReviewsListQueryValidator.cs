using FluentValidation;
using Reviews.Application.Common.Validation;
using Reviews.Domain.Entities;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public sealed class GetReviewsListQueryValidator : AbstractValidator<GetReviewsListQuery>
{
    public GetReviewsListQueryValidator()
    {
        RuleFor(x => x.CatalogItemId).GreaterThan(0).When(x => x.CatalogItemId is not null);
        RuleFor(x => x.RepairOrderId).GreaterThan(0).When(x => x.RepairOrderId is not null);
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId is not null);
        RuleFor(x => x.ReviewType).IsInEnum();
        RuleFor(x => x.MinRating).InclusiveBetween(Review.MinRating, Review.MaxRating).When(x => x.MinRating is not null);
        RuleFor(x => x.Search).MaximumLength(ValidationRules.MaxSearchLength);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ValidationRules.MaxPageSize);
    }
}

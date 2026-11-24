using FluentValidation;
using Reviews.Application.Common.Validation;

namespace Reviews.Application.Reviews.Queries.GetRepairOrderReviews;

public sealed class GetRepairOrderReviewsQueryValidator : AbstractValidator<GetRepairOrderReviewsQuery>
{
    public GetRepairOrderReviewsQueryValidator()
    {
        RuleFor(x => x.RepairOrderId).GreaterThan(0);
        RuleFor(x => x.CatalogItemIds).Must(ids => ids.Count <= GetRepairOrderReviewsQuery.MaxCatalogItems)
            .WithMessage($"At most {GetRepairOrderReviewsQuery.MaxCatalogItems} catalog items can be requested");
        RuleForEach(x => x.CatalogItemIds).GreaterThan(0);
        RuleFor(x => x.RepairReviewsLimit).InclusiveBetween(1, ValidationRules.MaxPageSize);
        RuleFor(x => x.ItemReviewsLimit).InclusiveBetween(1, ValidationRules.MaxPageSize);
    }
}

using FluentValidation;
using Reviews.Application.Common.Validation;

namespace Reviews.Application.Reviews.Queries.GetReviewComments;

public sealed class GetReviewCommentsQueryValidator : AbstractValidator<GetReviewCommentsQuery>
{
    public GetReviewCommentsQueryValidator()
    {
        RuleFor(x => x.ReviewId).MustBeObjectId();
    }
}

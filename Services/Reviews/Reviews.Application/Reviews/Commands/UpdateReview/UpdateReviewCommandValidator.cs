using FluentValidation;
using Reviews.Application.Common.Validation;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.Id).MustBeObjectId();
        RuleFor(x => x.ExpectedVersion).MustBeExpectedVersion();
        RuleFor(x => x.Rating).MustBeRating();
        RuleFor(x => x.Title).MustBeReviewTitle();
        RuleFor(x => x.Text).MustBeReviewText();
        RuleFor(x => x.Pros).MustBeShortPhrases();
        RuleFor(x => x.Cons).MustBeShortPhrases();
    }
}

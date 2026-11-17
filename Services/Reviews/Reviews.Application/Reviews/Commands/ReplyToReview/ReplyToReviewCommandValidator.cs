using FluentValidation;
using Reviews.Application.Common.Validation;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.ReplyToReview;

public sealed class ReplyToReviewCommandValidator : AbstractValidator<ReplyToReviewCommand>
{
    public ReplyToReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).MustBeObjectId();
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(StaffReply.MaxAuthorNameLength);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(StaffReply.MaxTextLength);
    }
}

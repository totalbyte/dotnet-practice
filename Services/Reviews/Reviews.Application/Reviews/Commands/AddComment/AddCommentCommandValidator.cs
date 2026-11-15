using FluentValidation;
using Reviews.Application.Common.Validation;
using Reviews.Domain.Entities;

namespace Reviews.Application.Reviews.Commands.AddComment;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.ReviewId).MustBeObjectId();
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(Comment.MaxAuthorNameLength);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Comment.MaxTextLength);
    }
}

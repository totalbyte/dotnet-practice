using AutoMapper;
using MediatR;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.AddComment;

public sealed class AddCommentCommandHandler(IReviewRepository reviews, IMapper mapper)
    : IRequestHandler<AddCommentCommand, CommentDto>
{
    public async Task<CommentDto> Handle(AddCommentCommand command, CancellationToken cancellationToken)
    {
        var review = await reviews.GetRequiredAsync(command.ReviewId, cancellationToken);

        var comment = review.AddComment(command.AuthorName, command.Text, command.IsStaff);
        await reviews.UpdateAsync(review, cancellationToken);

        return mapper.Map<CommentDto>(comment);
    }
}

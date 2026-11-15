using AutoMapper;
using MediatR;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.ReplyToReview;

public sealed class ReplyToReviewCommandHandler(IReviewRepository reviews, IMapper mapper)
    : IRequestHandler<ReplyToReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(ReplyToReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await reviews.GetRequiredAsync(command.ReviewId, cancellationToken);

        review.ReplyAsStaff(command.AuthorName, command.Text);
        await reviews.UpdateAsync(review, cancellationToken);

        return mapper.Map<ReviewDto>(review);
    }
}

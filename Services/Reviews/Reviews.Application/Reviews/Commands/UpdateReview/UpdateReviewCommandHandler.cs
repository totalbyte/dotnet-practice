using AutoMapper;
using MediatR;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewCommandHandler(IReviewRepository reviews, IMapper mapper)
    : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(UpdateReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await reviews.GetRequiredAsync(command.Id, cancellationToken);
        review.EnsureVersion(command.ExpectedVersion);

        review.Edit(command.Rating, command.Title, command.Text, command.Pros, command.Cons);
        await reviews.UpdateAsync(review, cancellationToken);

        return mapper.Map<ReviewDto>(review);
    }
}

using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Reviews.Application.Common.Diagnostics;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.ReplyToReview;

public sealed class ReplyToReviewCommandHandler(
    IReviewRepository reviews, IMapper mapper, ILogger<ReplyToReviewCommandHandler> logger)
    : IRequestHandler<ReplyToReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(ReplyToReviewCommand command, CancellationToken cancellationToken)
    {
        using var activity = ReviewsTelemetry.ActivitySource.StartActivity("ReplyToReview");
        activity?.SetTag("review.id", command.ReviewId);

        var review = await reviews.GetRequiredAsync(command.ReviewId, cancellationToken);

        review.ReplyAsStaff(command.AuthorName, command.Text);
        await reviews.UpdateAsync(review, cancellationToken);

        logger.LogInformation("Staff replied to review {ReviewId}, version {Version}", command.ReviewId, review.Version);

        return mapper.Map<ReviewDto>(review);
    }
}

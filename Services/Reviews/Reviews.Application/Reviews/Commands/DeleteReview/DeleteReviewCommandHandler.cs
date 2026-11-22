using MediatR;
using Microsoft.Extensions.Logging;
using Reviews.Application.Common.Diagnostics;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandHandler(IReviewRepository reviews, ILogger<DeleteReviewCommandHandler> logger)
    : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand command, CancellationToken cancellationToken)
    {
        using var activity = ReviewsTelemetry.ActivitySource.StartActivity("DeleteReview");
        activity?.SetTag("review.id", command.Id);

        var review = await reviews.GetRequiredAsync(command.Id, cancellationToken);
        review.EnsureVersion(command.ExpectedVersion);

        review.Delete();
        await reviews.UpdateAsync(review, cancellationToken);

        logger.LogInformation("Review {ReviewId} marked as deleted", command.Id);
    }
}

using MediatR;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandHandler(IReviewRepository reviews) : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await reviews.GetRequiredAsync(command.Id, cancellationToken);
        review.EnsureVersion(command.ExpectedVersion);

        review.Delete();
        await reviews.UpdateAsync(review, cancellationToken);
    }
}

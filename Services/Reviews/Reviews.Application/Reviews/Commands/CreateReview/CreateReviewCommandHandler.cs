using MediatR;
using Microsoft.Extensions.Logging;
using Reviews.Application.Common.Diagnostics;
using Reviews.Domain.Entities;
using Reviews.Domain.Enums;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandHandler(IReviewRepository reviews, ILogger<CreateReviewCommandHandler> logger)
    : IRequestHandler<CreateReviewCommand, string>
{
    public async Task<string> Handle(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        using var activity = ReviewsTelemetry.ActivitySource.StartActivity("CreateReview");
        activity?.SetTag("review.type", command.ReviewType.ToString());
        activity?.SetTag("catalog_item.id", command.CatalogItemId);
        activity?.SetTag("repair_order.id", command.RepairOrderId);

        var author = new Author(command.CustomerId, command.AuthorName);

        var review = command.ReviewType switch
        {
            ReviewType.Item => Review.ForItem(
                command.CatalogItemId.GetValueOrDefault(), command.ItemName!, author,
                command.Rating, command.Title, command.Text, command.Pros, command.Cons),
            ReviewType.Repair => Review.ForRepair(
                command.RepairOrderId.GetValueOrDefault(),
                new Device(command.Device!.Type, command.Device.Brand, command.Device.Model),
                author, command.Rating, command.Title, command.Text),
            _ => throw new DomainException("Review type is required")
        };

        await reviews.AddAsync(review, cancellationToken);

        activity?.SetTag("review.id", review.Id.ToString());
        logger.LogInformation(
            "Review {ReviewId} of type {ReviewType} created by customer {CustomerId} with rating {Rating} for catalog item {CatalogItemId} / repair order {RepairOrderId}",
            review.Id, review.ReviewType, command.CustomerId, review.Rating, review.CatalogItemId, review.RepairOrderId);

        return review.Id.ToString();
    }
}

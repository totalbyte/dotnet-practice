using MediatR;
using Reviews.Domain.Entities;
using Reviews.Domain.Enums;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandHandler(IReviewRepository reviews)
    : IRequestHandler<CreateReviewCommand, string>
{
    public async Task<string> Handle(CreateReviewCommand command, CancellationToken cancellationToken)
    {
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
        return review.Id.ToString();
    }
}

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Reviews.Dtos;
using Reviews.Application.Reviews.Queries.GetRepairOrderReviews;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Domain.Exceptions;
using Reviews.Grpc;
using FluentValidationException = FluentValidation.ValidationException;
using ReviewType = Reviews.Grpc.ReviewType;

namespace Reviews.WebApi.Grpc;

public sealed class ReviewsGrpcService(ISender sender) : ReviewsService.ReviewsServiceBase
{
    public override async Task<ReviewReply> GetReview(GetReviewRequest request, ServerCallContext context)
    {
        if (!ObjectId.TryParse(request.Id, out _))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be a 24-character hexadecimal ObjectId"));

        var review = await SendAsync(new GetReviewByIdQuery(request.Id), context.CancellationToken);
        return new ReviewReply
        {
            Id = review.Id,
            ReviewType = ToReviewType(review.ReviewType),
            Rating = review.Rating,
            Text = review.Text,
            Author = ToAuthor(review.Author),
            CommentsCount = review.CommentsCount,
            CreatedAt = ToTimestamp(review.CreatedAt)
        }.WithOptional(review.Title, review.CatalogItemId, review.RepairOrderId);
    }

    public override async Task<ReviewsReply> GetRepairOrderReviews(
        GetRepairOrderReviewsRequest request, ServerCallContext context)
    {
        if (request.RepairOrderId <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Repair order id must be positive"));

        var query = new GetRepairOrderReviewsQuery(
            request.RepairOrderId,
            [.. request.CatalogItemIds],
            request.RepairReviewsLimit == 0 ? GetRepairOrderReviewsQuery.DefaultRepairReviewsLimit : request.RepairReviewsLimit,
            request.ItemReviewsLimit == 0 ? GetRepairOrderReviewsQuery.DefaultItemReviewsLimit : request.ItemReviewsLimit);

        var reviews = await SendAsync(query, context.CancellationToken);
        return new ReviewsReply { Reviews = { reviews.Select(ToReply) } };
    }

    private async Task<T> SendAsync<T>(MediatR.IRequest<T> query, CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(query, cancellationToken);
        }
        catch (FluentValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                string.Join("; ", ex.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}"))));
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex) when (ex is DomainException or ConflictException)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    private static ReviewReply ToReply(ReviewSummaryDto review) => new ReviewReply
    {
        Id = review.Id,
        ReviewType = ToReviewType(review.ReviewType),
        Rating = review.Rating,
        Text = review.Text,
        Author = ToAuthor(review.Author),
        CommentsCount = review.CommentsCount,
        CreatedAt = ToTimestamp(review.CreatedAt)
    }.WithOptional(review.Title, review.CatalogItemId, review.RepairOrderId);

    private static ReviewType ToReviewType(Domain.Enums.ReviewType type) => type switch
    {
        Domain.Enums.ReviewType.Repair => ReviewType.Repair,
        Domain.Enums.ReviewType.Item => ReviewType.Item,
        _ => ReviewType.Unspecified
    };

    private static Author ToAuthor(AuthorDto author)
        => new() { CustomerId = author.CustomerId, DisplayName = author.DisplayName };

    private static Timestamp ToTimestamp(DateTime utc)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}

internal static class ReviewReplyExtensions
{
    public static ReviewReply WithOptional(this ReviewReply reply, string? title, long? catalogItemId, long? repairOrderId)
    {
        if (title is not null)
            reply.Title = title;
        if (catalogItemId is { } itemId)
            reply.CatalogItemId = itemId;
        if (repairOrderId is { } orderId)
            reply.RepairOrderId = orderId;
        return reply;
    }
}

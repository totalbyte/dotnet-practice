using FluentValidation.Results;
using Grpc.Core;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Reviews.Dtos;
using Reviews.Application.Reviews.Queries.GetRepairOrderReviews;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Grpc;
using Reviews.WebApi.Grpc;
using DomainReviewType = Reviews.Domain.Enums.ReviewType;

namespace Reviews.Tests;

public class ReviewsGrpcServiceTests
{
    private const string ReviewId = "671000000000000000000004";

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ReviewsGrpcService _service;
    private readonly ServerCallContext _context = new TestServerCallContext("/reviews.ReviewsService/GetReview");

    public ReviewsGrpcServiceTests() => _service = new ReviewsGrpcService(_sender);

    [Fact]
    public async Task GetReview_MalformedId_ReturnsInvalidArgumentWithoutQuery()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => _service.GetReview(new GetReviewRequest { Id = "not-an-object-id" }, _context));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        await _sender.DidNotReceiveWithAnyArgs().Send(Arg.Any<GetReviewByIdQuery>());
    }

    [Fact]
    public async Task GetReview_MissingReview_ReturnsNotFound()
    {
        _sender.Send(Arg.Any<GetReviewByIdQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new NotFoundException("Review", ReviewId));

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => _service.GetReview(new GetReviewRequest { Id = ReviewId }, _context));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetRepairOrderReviews_ValidationFailure_ReturnsInvalidArgument()
    {
        _sender.Send(Arg.Any<GetRepairOrderReviewsQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FluentValidation.ValidationException(
                [new ValidationFailure("CatalogItemIds[0]", "'Catalog Item Ids' must be greater than '0'.")]));

        var exception = await Assert.ThrowsAsync<RpcException>(() => _service.GetRepairOrderReviews(
            new GetRepairOrderReviewsRequest { RepairOrderId = 1, CatalogItemIds = { 0 } }, _context));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task GetRepairOrderReviews_UsesDefaultLimitsAndMapsReviews()
    {
        GetRepairOrderReviewsQuery? sent = null;
        _sender.Send(Arg.Do<GetRepairOrderReviewsQuery>(query => sent = query), Arg.Any<CancellationToken>())
            .Returns(new List<ReviewSummaryDto>
            {
                new(ReviewId, DomainReviewType.Item, 2, null, "Green line", new AuthorDto(4, "Taras"), 1, "Screen",
                    null, 0, new DateTime(2026, 11, 5, 20, 45, 0), null, 1)
            });

        var reply = await _service.GetRepairOrderReviews(
            new GetRepairOrderReviewsRequest { RepairOrderId = 1, CatalogItemIds = { 1, 6 } }, _context);

        Assert.NotNull(sent);
        Assert.Equal(GetRepairOrderReviewsQuery.DefaultRepairReviewsLimit, sent.RepairReviewsLimit);
        Assert.Equal(GetRepairOrderReviewsQuery.DefaultItemReviewsLimit, sent.ItemReviewsLimit);
        var review = Assert.Single(reply.Reviews);
        Assert.Equal(ReviewType.Item, review.ReviewType);
        Assert.False(review.HasTitle);
        Assert.Equal(1, review.CatalogItemId);
        Assert.False(review.HasRepairOrderId);
    }
}

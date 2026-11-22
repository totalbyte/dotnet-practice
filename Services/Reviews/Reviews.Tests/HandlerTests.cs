using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Common.Mappings;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Commands.UpdateReview;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;
using Reviews.Domain.Enums;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Tests;

public sealed class HandlerTests
{
    private readonly IReviewRepository _reviews = Substitute.For<IReviewRepository>();

    private static IMapper Mapper { get; } = new MapperConfiguration(
        cfg => cfg.AddProfile<ReviewProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Create_adds_review_and_returns_string_object_id()
    {
        var command = new CreateReviewCommand(
            ReviewType.Repair, 1, "Olena", 5, null, "Fixed", RepairOrderId: 9, Device: new DeviceDto("TV", "LG", "X"));

        var id = await new CreateReviewCommandHandler(_reviews, NullLogger<CreateReviewCommandHandler>.Instance).Handle(command, CancellationToken.None);

        Assert.True(ObjectId.TryParse(id, out _));
        await _reviews.Received(1).AddAsync(
            Arg.Is<Review>(r => r.Id.ToString() == id && r.Device!.Model == "X"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_with_stale_version_throws_conflict_without_writing()
    {
        var review = Review.ForItem(1, "Screen", new Author(1, "Olena"), 4, null, "Good");
        review.BumpVersion();
        _reviews.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);

        var command = new UpdateReviewCommand(review.Id.ToString(), 1, 5, null, "Better");

        await Assert.ThrowsAsync<ConflictException>(
            () => new UpdateReviewCommandHandler(_reviews, Mapper).Handle(command, CancellationToken.None));
        await _reviews.DidNotReceive().UpdateAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_of_missing_review_throws_not_found()
    {
        var command = new UpdateReviewCommand(ObjectId.GenerateNewId().ToString(), 1, 5, null, "Better");

        await Assert.ThrowsAsync<NotFoundException>(
            () => new UpdateReviewCommandHandler(_reviews, Mapper).Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Update_with_current_version_saves_and_returns_mapped_review()
    {
        var review = Review.ForItem(1, "Screen", new Author(1, "Olena"), 4, null, "Good");
        _reviews.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);

        var dto = await new UpdateReviewCommandHandler(_reviews, Mapper).Handle(
            new UpdateReviewCommand(review.Id.ToString(), 1, 5, "Title", "Better", ["Bright"]), CancellationToken.None);

        Assert.Equal(5, dto.Rating);
        Assert.Equal(["Bright"], dto.Pros);
        await _reviews.Received(1).UpdateAsync(review, Arg.Any<CancellationToken>());
    }
}

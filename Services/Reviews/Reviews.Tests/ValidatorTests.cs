using NSubstitute;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Commands.UpdateReview;
using Reviews.Application.Reviews.Dtos;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsList;
using Reviews.Domain.Enums;
using Reviews.Domain.Interfaces;

namespace Reviews.Tests;

public sealed class ValidatorTests
{
    private readonly IReviewRepository _reviews = Substitute.For<IReviewRepository>();

    private static CreateReviewCommand ItemCommand(int rating = 5) => new(
        ReviewType.Item, 2, "Andrii", rating, null, "Great part", CatalogItemId: 4, ItemName: "SSD");

    [Fact]
    public async Task Valid_item_review_passes()
    {
        var result = await new CreateReviewCommandValidator(_reviews).ValidateAsync(ItemCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Rating_ten_is_rejected()
    {
        var result = await new CreateReviewCommandValidator(_reviews).ValidateAsync(ItemCommand(rating: 10));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReviewCommand.Rating));
    }

    [Fact]
    public async Task Second_review_of_same_item_by_same_customer_is_rejected_by_database_check()
    {
        _reviews.ExistsForCatalogItemAsync(4, 2, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new CreateReviewCommandValidator(_reviews).ValidateAsync(ItemCommand());

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateReviewCommand.CatalogItemId), error.PropertyName);
        Assert.Equal("The customer has already reviewed this catalog item", error.ErrorMessage);
    }

    [Fact]
    public async Task Repair_review_requires_device_and_rejects_item_fields()
    {
        var command = new CreateReviewCommand(
            ReviewType.Repair, 1, "Olena", 5, null, "Fixed", CatalogItemId: 1, RepairOrderId: 7);

        var result = await new CreateReviewCommandValidator(_reviews).ValidateAsync(command);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReviewCommand.Device));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReviewCommand.CatalogItemId));
    }

    [Fact]
    public async Task Repair_order_reviewed_twice_is_rejected()
    {
        _reviews.ExistsForRepairOrderAsync(7, Arg.Any<CancellationToken>()).Returns(true);
        var command = new CreateReviewCommand(
            ReviewType.Repair, 1, "Olena", 5, null, "Fixed", RepairOrderId: 7, Device: new DeviceDto("TV", null, "X"));

        var result = await new CreateReviewCommandValidator(_reviews).ValidateAsync(command);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "This repair order has already been reviewed");
    }

    [Theory]
    [InlineData("not-an-id")]
    [InlineData("67100000000000000000000Z")]
    [InlineData("")]
    public void Invalid_object_id_is_rejected(string id)
    {
        var result = new GetReviewByIdQueryValidator().Validate(new GetReviewByIdQuery(id));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Update_without_if_match_version_is_rejected()
    {
        var command = new UpdateReviewCommand("671000000000000000000002", 0, 5, null, "Text");

        var result = new UpdateReviewCommandValidator().Validate(command);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReviewCommand.ExpectedVersion));
    }

    [Fact]
    public void Page_size_above_limit_is_rejected()
    {
        var result = new GetReviewsListQueryValidator().Validate(new GetReviewsListQuery(PageSize: 500));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetReviewsListQuery.PageSize));
    }
}

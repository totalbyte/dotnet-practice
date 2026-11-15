using Reviews.Domain.Entities;
using Reviews.Domain.Enums;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Tests;

public sealed class ReviewTests
{
    private static readonly Author Author = new(1, "Olena");

    private static Review ItemReview() => Review.ForItem(1, "OLED screen", Author, 4, "Good", "Bright colors");

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(10)]
    public void Creating_review_with_rating_out_of_range_throws_domain_exception(int rating)
    {
        var ex = Assert.Throws<DomainException>(
            () => Review.ForItem(1, "OLED screen", Author, rating, null, "Text"));

        Assert.Equal("Rating must be between 1 and 5", ex.Message);
    }

    [Fact]
    public void New_review_has_identity_version_one_and_creation_time()
    {
        var review = ItemReview();

        Assert.NotEqual(default, review.Id);
        Assert.Equal(1, review.Version);
        Assert.Null(review.UpdatedAt);
        Assert.Equal(ReviewType.Item, review.ReviewType);
    }

    [Fact]
    public void Repair_review_requires_device_and_rejects_pros()
    {
        Assert.Throws<DomainException>(() => Review.ForRepair(5, null!, Author, 5, null, "Fixed"));

        var review = Review.ForRepair(5, new Device("TV", "LG", "43UP7500"), Author, 5, null, "Fixed");

        Assert.Throws<DomainException>(() => review.Edit(4, null, "Fixed", ["Fast"], null));
    }

    [Fact]
    public void Only_last_twenty_comments_are_kept_but_all_are_counted()
    {
        var review = ItemReview();

        for (var i = 1; i <= 25; i++)
            review.AddComment("Reader", $"Comment {i}", isStaff: false);

        Assert.Equal(Review.MaxStoredComments, review.Comments.Count);
        Assert.Equal(25, review.CommentsCount);
        Assert.Equal("Comment 6", review.Comments.First().Text);
        Assert.NotNull(review.UpdatedAt);
    }

    [Fact]
    public void Empty_comment_is_rejected()
    {
        var review = ItemReview();

        Assert.Throws<DomainException>(() => review.AddComment("Reader", "  ", isStaff: false));
        Assert.Equal(0, review.CommentsCount);
    }

    [Fact]
    public void Deleted_review_cannot_be_changed()
    {
        var review = ItemReview();
        review.Delete();

        Assert.True(review.IsDeleted);
        Assert.Throws<DomainException>(() => review.Edit(5, null, "Text", null, null));
        Assert.Throws<DomainException>(() => review.Delete());
    }

    [Fact]
    public void Version_mismatch_is_reported_as_conflict()
    {
        var review = ItemReview();
        review.BumpVersion();

        review.EnsureVersion(2);
        Assert.Throws<ConflictException>(() => review.EnsureVersion(1));
    }

    [Fact]
    public void Staff_reply_is_stored_as_value_object()
    {
        var review = ItemReview();

        review.ReplyAsStaff("Service center", "Thank you");

        Assert.Equal("Service center", review.StaffReply!.AuthorName);
        Assert.Throws<DomainException>(() => review.ReplyAsStaff("Service center", ""));
    }
}

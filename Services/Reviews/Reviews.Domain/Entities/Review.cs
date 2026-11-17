using Reviews.Domain.Common;
using Reviews.Domain.Enums;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public sealed class Review : BaseEntity
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int MaxTitleLength = 200;
    public const int MaxTextLength = 5000;
    public const int MaxItemNameLength = 200;
    public const int MaxStoredComments = 20;
    public const int MaxListEntries = 10;
    public const int MaxListEntryLength = 200;

    private List<string> _pros = [];
    private List<string> _cons = [];
    private List<Comment> _comments = [];

    private Review()
    {
    }

    private Review(ReviewType reviewType, Author author, int rating, string? title, string text)
    {
        ReviewType = reviewType;
        Author = author ?? throw new DomainException("Author is required");
        SetContent(rating, title, text);
    }

    public ReviewType ReviewType { get; private set; }

    public int Rating { get; private set; }

    public string? Title { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public Author Author { get; private set; } = null!;

    public long? CatalogItemId { get; private set; }

    public string? ItemName { get; private set; }

    public long? RepairOrderId { get; private set; }

    public Device? Device { get; private set; }

    public IReadOnlyCollection<string> Pros => _pros.AsReadOnly();

    public IReadOnlyCollection<string> Cons => _cons.AsReadOnly();

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    public int CommentsCount { get; private set; }

    public StaffReply? StaffReply { get; private set; }

    public bool IsDeleted { get; private set; }

    public static Review ForItem(
        long catalogItemId,
        string itemName,
        Author author,
        int rating,
        string? title,
        string text,
        IEnumerable<string>? pros = null,
        IEnumerable<string>? cons = null)
    {
        var review = new Review(ReviewType.Item, author, rating, title, text)
        {
            CatalogItemId = Guard.Positive(catalogItemId, "CatalogItemId"),
            ItemName = Guard.Required(itemName, "Item name", MaxItemNameLength)
        };
        review.SetProsAndCons(pros, cons);
        return review;
    }

    public static Review ForRepair(long repairOrderId, Device device, Author author, int rating, string? title, string text)
        => new(ReviewType.Repair, author, rating, title, text)
        {
            RepairOrderId = Guard.Positive(repairOrderId, "RepairOrderId"),
            Device = device ?? throw new DomainException("Device is required for a repair review")
        };

    public void Edit(int rating, string? title, string text, IEnumerable<string>? pros, IEnumerable<string>? cons)
    {
        EnsureNotDeleted();
        SetContent(rating, title, text);
        SetProsAndCons(pros, cons);
        Touch();
    }

    public Comment AddComment(string authorName, string text, bool isStaff)
    {
        EnsureNotDeleted();

        var comment = new Comment(authorName, text, isStaff, Clock.UtcNow);
        _comments.Add(comment);
        if (_comments.Count > MaxStoredComments)
            _comments.RemoveRange(0, _comments.Count - MaxStoredComments);

        CommentsCount++;
        Touch();
        return comment;
    }

    public void ReplyAsStaff(string authorName, string text)
    {
        EnsureNotDeleted();
        StaffReply = new StaffReply(authorName, text, Clock.UtcNow);
        Touch();
    }

    public void Delete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        Touch();
    }

    private void SetContent(int rating, string? title, string text)
    {
        if (rating is < MinRating or > MaxRating)
            throw new DomainException($"Rating must be between {MinRating} and {MaxRating}");

        Rating = rating;
        Title = Guard.Optional(title, "Title", MaxTitleLength);
        Text = Guard.Required(text, "Text", MaxTextLength);
    }

    private void SetProsAndCons(IEnumerable<string>? pros, IEnumerable<string>? cons)
    {
        var prosList = NormalizeList(pros, "Pros");
        var consList = NormalizeList(cons, "Cons");

        if (ReviewType == ReviewType.Repair && (prosList.Count > 0 || consList.Count > 0))
            throw new DomainException("Pros and cons are available only for catalog item reviews");

        _pros = prosList;
        _cons = consList;
    }

    private static List<string> NormalizeList(IEnumerable<string>? values, string name)
    {
        var list = (values ?? []).Select(value => Guard.Required(value, $"{name} entry", MaxListEntryLength)).ToList();
        if (list.Count > MaxListEntries)
            throw new DomainException($"{name} must not contain more than {MaxListEntries} entries");

        return list;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Review has been deleted");
    }
}

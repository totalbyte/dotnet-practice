using Reviews.Domain.Common;

namespace Reviews.Domain.ValueObjects;

public sealed class StaffReply : ValueObject
{
    public const int MaxAuthorNameLength = 200;
    public const int MaxTextLength = 2000;

    public StaffReply(string authorName, string text, DateTime repliedAt)
    {
        AuthorName = Guard.Required(authorName, "Reply author", MaxAuthorNameLength);
        Text = Guard.Required(text, "Reply text", MaxTextLength);
        RepliedAt = repliedAt;
    }

    public string AuthorName { get; }

    public string Text { get; }

    public DateTime RepliedAt { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AuthorName;
        yield return Text;
        yield return RepliedAt;
    }
}

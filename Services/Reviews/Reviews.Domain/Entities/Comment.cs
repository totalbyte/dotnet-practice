using Reviews.Domain.Common;

namespace Reviews.Domain.Entities;

public sealed class Comment
{
    public const int MaxAuthorNameLength = 200;
    public const int MaxTextLength = 1000;

    public Comment(string authorName, string text, bool isStaff, DateTime createdAt)
    {
        AuthorName = Guard.Required(authorName, "Comment author", MaxAuthorNameLength);
        Text = Guard.Required(text, "Comment text", MaxTextLength);
        IsStaff = isStaff;
        CreatedAt = createdAt;
    }

    public string AuthorName { get; private set; }

    public string Text { get; private set; }

    public bool IsStaff { get; private set; }

    public DateTime CreatedAt { get; private set; }
}

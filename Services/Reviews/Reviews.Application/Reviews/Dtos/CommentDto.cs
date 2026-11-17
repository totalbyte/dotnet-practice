namespace Reviews.Application.Reviews.Dtos;

public sealed record CommentDto(string AuthorName, string Text, bool IsStaff, DateTime CreatedAt);

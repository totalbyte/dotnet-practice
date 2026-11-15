namespace Reviews.Application.Reviews.Dtos;

public sealed record StaffReplyDto(string AuthorName, string Text, DateTime RepliedAt);

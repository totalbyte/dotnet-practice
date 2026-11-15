namespace Reviews.WebApi.Contracts;

public sealed record AddCommentRequest(string AuthorName, string Text, bool IsStaff = false);

namespace Reviews.WebApi.Contracts;

public sealed record UpdateReviewRequest(
    int Rating,
    string? Title,
    string Text,
    IReadOnlyList<string>? Pros = null,
    IReadOnlyList<string>? Cons = null);

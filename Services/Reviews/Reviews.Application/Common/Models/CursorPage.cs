namespace Reviews.Application.Common.Models;

public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor)
{
    public bool HasMore => NextCursor is not null;
}

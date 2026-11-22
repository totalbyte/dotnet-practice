namespace Platform.Aggregator.Clients.Contracts;

public sealed record ReviewResponse
{
    public string Id { get; init; } = string.Empty;
    public string ReviewType { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string Text { get; init; } = string.Empty;
    public ReviewAuthorResponse? Author { get; init; }
    public long? CatalogItemId { get; init; }
    public long? RepairOrderId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record ReviewAuthorResponse(long CustomerId, string DisplayName);

public sealed record ReviewPageResponse(IReadOnlyList<ReviewResponse> Items, long TotalCount);

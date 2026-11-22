using Platform.Aggregator.Clients.Contracts;

namespace Platform.Aggregator.Clients;

public sealed class CatalogClient(HttpClient http, ILogger<CatalogClient> logger) : ServiceClient(http, logger)
{
    public override string ServiceName => "catalog-api";

    public Task<CatalogItemResponse?> GetItemAsync(long id, CancellationToken cancellationToken)
        => GetOrDefaultAsync<CatalogItemResponse>($"/api/catalog-items/{id}", $"catalog item {id}", cancellationToken);

    public async Task<IReadOnlyList<CatalogItemResponse>> GetItemsAsync(
        IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        var items = await Task.WhenAll(ids.Select(id => GetItemAsync(id, cancellationToken)));
        return [.. items.OfType<CatalogItemResponse>()];
    }
}

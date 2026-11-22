using Platform.Aggregator.Clients.Contracts;

namespace Platform.Aggregator.Clients;

public sealed class RepairsClient(HttpClient http, ILogger<RepairsClient> logger) : ServiceClient(http, logger)
{
    public override string ServiceName => "repairs-api";

    public Task<RepairOrderResponse?> GetRepairOrderAsync(long id, CancellationToken cancellationToken)
        => GetOrDefaultAsync<RepairOrderResponse>($"/api/repair-orders/{id}", $"repair order {id}", cancellationToken);
}

using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Services;
using Catalog.Domain.Entities;
using Platform.ServiceDefaults.Metrics;

namespace Catalog.Api.Metrics;

public sealed class ActiveCatalogItemsSnapshot(
    IServiceScopeFactory scopes,
    CatalogMetrics metrics,
    ILogger<ActiveCatalogItemsSnapshot> logger) : MetricsSnapshotService(scopes, logger)
{
    protected override async Task RefreshAsync(IServiceProvider services, CancellationToken ct)
    {
        var items = services.GetRequiredService<CatalogItemService>();
        var counts = new Dictionary<string, long>();

        foreach (var itemType in Enum.GetValues<CatalogItemType>())
        {
            var page = await items.GetListAsync(
                new CatalogItemQuery { ItemType = itemType, IsActive = true, PageSize = 1 }, ct);
            counts[itemType.ToString()] = page.TotalCount;
        }

        metrics.UpdateActiveItems(counts);
    }
}

using Platform.ServiceDefaults.Metrics;
using Repairs.Bll.Dtos.RepairOrders;
using Repairs.Bll.Interfaces;
using Repairs.Domain.Constants;

namespace Repairs.Api.Metrics;

public sealed class OpenRepairOrdersSnapshot(
    IServiceScopeFactory scopes,
    RepairOrderMetrics metrics,
    ILogger<OpenRepairOrdersSnapshot> logger) : MetricsSnapshotService(scopes, logger)
{
    public static readonly IReadOnlyList<string> OpenStatuses =
    [
        RepairOrderStatuses.Accepted, RepairOrderStatuses.Diagnosed, RepairOrderStatuses.Approved,
        RepairOrderStatuses.InRepair, RepairOrderStatuses.Ready, RepairOrderStatuses.Paid
    ];

    protected override async Task RefreshAsync(IServiceProvider services, CancellationToken ct)
    {
        var repairOrders = services.GetRequiredService<IRepairOrderService>();
        var counts = new Dictionary<string, long>();

        foreach (var status in OpenStatuses)
        {
            var page = await repairOrders.SearchAsync(new RepairOrderQueryDto { Status = status, PageSize = 1 }, ct);
            counts[status] = page.TotalCount;
        }

        metrics.UpdateOpenOrders(counts);
    }
}

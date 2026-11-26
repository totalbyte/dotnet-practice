using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Repairs.Api.Metrics;

public sealed class RepairOrderMetrics
{
    public const string MeterName = "Repairs.Api";
    public const string Success = "success";
    public const string Failure = "failure";

    private readonly Counter<long> _created;
    private readonly Counter<long> _statusChanged;
    private readonly Counter<long> _itemsAdded;
    private readonly Histogram<double> _operationDuration;
    private IReadOnlyDictionary<string, long> _openOrders = new Dictionary<string, long>();

    public RepairOrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _created = meter.CreateCounter<long>(
            "repair_orders.created", "{order}", "Repair orders accepted from customers");
        _statusChanged = meter.CreateCounter<long>(
            "repair_orders.status_changed", "{order}", "Status transitions of repair orders");
        _itemsAdded = meter.CreateCounter<long>(
            "repair_orders.items_added", "{item}", "Parts and services added to repair orders");
        _operationDuration = meter.CreateHistogram<double>(
            "repair_orders.operation.duration", "ms", "Duration of repair order operations");

        meter.CreateObservableGauge("repair_orders.open", ObserveOpenOrders,
            "{order}", "Repair orders not yet issued or cancelled, by status");
    }

    public void OrderCreated(string deviceType)
        => _created.Add(1, new KeyValuePair<string, object?>("device.type", deviceType));

    public void StatusChanged(string status)
        => _statusChanged.Add(1, new KeyValuePair<string, object?>("repair_order.status", status));

    public void ItemAdded()
        => _itemsAdded.Add(1);

    public void RecordDuration(string operation, bool success, TimeSpan elapsed)
        => _operationDuration.Record(elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? Success : Failure));

    public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            var result = await action();
            RecordDuration(operation, success: true, Stopwatch.GetElapsedTime(startedAt));
            return result;
        }
        catch
        {
            RecordDuration(operation, success: false, Stopwatch.GetElapsedTime(startedAt));
            throw;
        }
    }

    public Task MeasureAsync(string operation, Func<Task> action)
        => MeasureAsync(operation, async () =>
        {
            await action();
            return true;
        });

    public void UpdateOpenOrders(IReadOnlyDictionary<string, long> countsByStatus)
        => Volatile.Write(ref _openOrders, countsByStatus);

    private IEnumerable<Measurement<long>> ObserveOpenOrders()
        => Volatile.Read(ref _openOrders).Select(pair =>
            new Measurement<long>(pair.Value, new KeyValuePair<string, object?>("repair_order.status", pair.Key)));
}

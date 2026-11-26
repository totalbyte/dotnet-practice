using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Catalog.Api.Metrics;

public sealed class CatalogMetrics
{
    public const string MeterName = "Catalog.Api";
    public const string Success = "success";
    public const string Failure = "failure";

    private readonly Counter<long> _created;
    private readonly Counter<long> _priceChanged;
    private readonly Counter<long> _deleted;
    private readonly Histogram<double> _operationDuration;
    private IReadOnlyDictionary<string, long> _activeItems = new Dictionary<string, long>();

    public CatalogMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _created = meter.CreateCounter<long>(
            "catalog_items.created", "{item}", "Parts and services added to the catalog");
        _priceChanged = meter.CreateCounter<long>(
            "catalog_items.price_changed", "{item}", "Price changes of catalog items");
        _deleted = meter.CreateCounter<long>(
            "catalog_items.deleted", "{item}", "Catalog items withdrawn from the catalog");
        _operationDuration = meter.CreateHistogram<double>(
            "catalog_items.operation.duration", "ms", "Duration of catalog item operations");

        meter.CreateObservableGauge("catalog_items.active", ObserveActiveItems,
            "{item}", "Catalog items available for orders, by item type");
    }

    public void ItemCreated(string itemType)
        => _created.Add(1, ItemType(itemType));

    public void PriceChanged(string itemType)
        => _priceChanged.Add(1, ItemType(itemType));

    public void ItemDeleted()
        => _deleted.Add(1);

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

    public void UpdateActiveItems(IReadOnlyDictionary<string, long> countsByType)
        => Volatile.Write(ref _activeItems, countsByType);

    private IEnumerable<Measurement<long>> ObserveActiveItems()
        => Volatile.Read(ref _activeItems).Select(pair => new Measurement<long>(pair.Value, ItemType(pair.Key)));

    private static KeyValuePair<string, object?> ItemType(string itemType) => new("catalog_item.type", itemType);
}

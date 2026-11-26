using System.Diagnostics.Metrics;
using Catalog.Api.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Catalog.Tests;

public sealed class CatalogMetricsTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IMeterFactory _meterFactory;
    private readonly CatalogMetrics _metrics;

    public CatalogMetricsTests()
    {
        _provider = new ServiceCollection().AddMetrics().AddSingleton<CatalogMetrics>().BuildServiceProvider();
        _meterFactory = _provider.GetRequiredService<IMeterFactory>();
        _metrics = _provider.GetRequiredService<CatalogMetrics>();
    }

    [Fact]
    public void Price_changes_are_counted_by_item_type()
    {
        using var collector = new MetricCollector<long>(_meterFactory, CatalogMetrics.MeterName, "catalog_items.price_changed");

        _metrics.PriceChanged("Part");

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("Part", measurement.Tags["catalog_item.type"]);
    }

    [Fact]
    public async Task Failed_operation_duration_is_recorded_and_the_error_rethrown()
    {
        using var collector = new MetricCollector<double>(_meterFactory, CatalogMetrics.MeterName,
            "catalog_items.operation.duration");

        await Assert.ThrowsAsync<TimeoutException>(() =>
            _metrics.MeasureAsync("update", () => Task.FromException(new TimeoutException())));

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(CatalogMetrics.Failure, measurement.Tags["status"]);
        Assert.Equal("update", measurement.Tags["operation"]);
    }

    [Fact]
    public void Active_items_gauge_reports_counts_by_type()
    {
        using var collector = new MetricCollector<long>(_meterFactory, CatalogMetrics.MeterName, "catalog_items.active");

        _metrics.UpdateActiveItems(new Dictionary<string, long> { ["Part"] = 7, ["Service"] = 3 });
        collector.RecordObservableInstruments();

        var values = collector.GetMeasurementSnapshot().ToDictionary(m => (string)m.Tags["catalog_item.type"]!, m => m.Value);
        Assert.Equal(7, values["Part"]);
        Assert.Equal(3, values["Service"]);
    }

    public void Dispose() => _provider.Dispose();
}

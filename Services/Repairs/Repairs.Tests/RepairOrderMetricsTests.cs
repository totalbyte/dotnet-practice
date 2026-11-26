using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Repairs.Api.Metrics;
using Repairs.Bll.Dtos;
using Repairs.Bll.Dtos.RepairOrders;
using Repairs.Bll.Interfaces;
using Repairs.Domain.Constants;

namespace Repairs.Tests;

public sealed class RepairOrderMetricsTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IMeterFactory _meterFactory;
    private readonly RepairOrderMetrics _metrics;

    public RepairOrderMetricsTests()
    {
        var repairOrders = Substitute.For<IRepairOrderService>();
        repairOrders.SearchAsync(Arg.Any<RepairOrderQueryDto>(), Arg.Any<CancellationToken>())
            .Returns(call => new PagedResultDto<RepairOrderSummaryDto>
            {
                TotalCount = call.Arg<RepairOrderQueryDto>().Status == RepairOrderStatuses.InRepair ? 4 : 1
            });

        _provider = new ServiceCollection()
            .AddMetrics()
            .AddSingleton<RepairOrderMetrics>()
            .AddScoped(_ => repairOrders)
            .BuildServiceProvider();
        _meterFactory = _provider.GetRequiredService<IMeterFactory>();
        _metrics = _provider.GetRequiredService<RepairOrderMetrics>();
    }

    [Fact]
    public void Created_orders_are_counted_by_device_type()
    {
        using var collector = Collector<long>("repair_orders.created");

        _metrics.OrderCreated(DeviceTypes.Laptop);
        _metrics.OrderCreated(DeviceTypes.Laptop);

        var measurements = collector.GetMeasurementSnapshot();
        Assert.Equal(2, measurements.EvaluateAsCounter());
        Assert.All(measurements, m => Assert.Equal(DeviceTypes.Laptop, m.Tags["device.type"]));
    }

    [Fact]
    public async Task Duration_is_recorded_for_successful_and_failed_operations()
    {
        using var collector = Collector<double>("repair_orders.operation.duration");

        await _metrics.MeasureAsync("create", () => Task.FromResult(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _metrics.MeasureAsync("change_status", () => Task.FromException(new InvalidOperationException())));

        var measurements = collector.GetMeasurementSnapshot();
        Assert.Collection(measurements,
            m => Assert.Equal(("create", RepairOrderMetrics.Success), (m.Tags["operation"], m.Tags["status"])),
            m => Assert.Equal(("change_status", RepairOrderMetrics.Failure), (m.Tags["operation"], m.Tags["status"])));
        Assert.All(measurements, m => Assert.True(m.Value >= 0));
    }

    [Fact]
    public async Task Open_orders_gauge_reports_the_last_snapshot_by_status()
    {
        using var collector = Collector<long>("repair_orders.open");
        var snapshot = new OpenRepairOrdersSnapshot(_provider.GetRequiredService<IServiceScopeFactory>(), _metrics,
            NullLogger<OpenRepairOrdersSnapshot>.Instance);

        collector.RecordObservableInstruments();
        Assert.Empty(collector.GetMeasurementSnapshot());

        await snapshot.RefreshOnceAsync(CancellationToken.None);
        collector.RecordObservableInstruments();

        var values = collector.GetMeasurementSnapshot()
            .ToDictionary(m => (string)m.Tags["repair_order.status"]!, m => m.Value);
        Assert.Equal(OpenRepairOrdersSnapshot.OpenStatuses.Order(), values.Keys.Order());
        Assert.Equal(4, values[RepairOrderStatuses.InRepair]);
        Assert.DoesNotContain(RepairOrderStatuses.Issued, values.Keys);
    }

    private MetricCollector<T> Collector<T>(string instrument) where T : struct
        => new(_meterFactory, RepairOrderMetrics.MeterName, instrument);

    public void Dispose() => _provider.Dispose();
}

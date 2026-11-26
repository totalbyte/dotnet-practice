using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Enums;
using Reviews.WebApi.Metrics;

namespace Reviews.Tests;

public sealed class ReviewMetricsTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IMeterFactory _meterFactory;
    private readonly ReviewMetrics _metrics;

    public ReviewMetricsTests()
    {
        var queries = Substitute.For<IReviewQueries>();
        queries.GetListAsync(Arg.Any<ReviewFilter>(), 1, 1, Arg.Any<CancellationToken>())
            .Returns(call => new PagedResult<ReviewSummaryDto>([], 1, 1,
                call.Arg<ReviewFilter>().ReviewType == ReviewType.Repair ? 5 : 2));

        _provider = new ServiceCollection()
            .AddMetrics()
            .AddSingleton<ReviewMetrics>()
            .AddScoped(_ => queries)
            .BuildServiceProvider();
        _meterFactory = _provider.GetRequiredService<IMeterFactory>();
        _metrics = _provider.GetRequiredService<ReviewMetrics>();
    }

    [Fact]
    public void Created_reviews_are_tagged_with_type_and_rating_only()
    {
        using var collector = new MetricCollector<long>(_meterFactory, ReviewMetrics.MeterName, "reviews.created");

        _metrics.ReviewCreated(nameof(ReviewType.Repair), 5);

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(["review.rating", "review.type"], measurement.Tags.Keys.Order());
        Assert.Equal("repair", measurement.Tags["review.type"]);
        Assert.Equal(5, measurement.Tags["review.rating"]);
    }

    [Fact]
    public async Task Published_reviews_gauge_is_filled_by_the_snapshot()
    {
        using var collector = new MetricCollector<long>(_meterFactory, ReviewMetrics.MeterName, "reviews.published");
        var snapshot = new PublishedReviewsSnapshot(_provider.GetRequiredService<IServiceScopeFactory>(), _metrics,
            NullLogger<PublishedReviewsSnapshot>.Instance);

        await snapshot.RefreshOnceAsync(CancellationToken.None);
        collector.RecordObservableInstruments();

        var values = collector.GetMeasurementSnapshot().ToDictionary(m => (string)m.Tags["review.type"]!, m => m.Value);
        Assert.Equal(5, values["repair"]);
        Assert.Equal(2, values["item"]);
    }

    public void Dispose() => _provider.Dispose();
}

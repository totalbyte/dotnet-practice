using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Reviews.WebApi.Metrics;

public sealed class ReviewMetrics
{
    public const string MeterName = "Reviews.WebApi";
    public const string Success = "success";
    public const string Failure = "failure";

    private readonly Counter<long> _created;
    private readonly Counter<long> _replied;
    private readonly Counter<long> _commentsAdded;
    private readonly Histogram<double> _operationDuration;
    private IReadOnlyDictionary<string, long> _publishedReviews = new Dictionary<string, long>();

    public ReviewMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _created = meter.CreateCounter<long>(
            "reviews.created", "{review}", "Reviews left by customers");
        _replied = meter.CreateCounter<long>(
            "reviews.replied", "{review}", "Reviews answered by the service center staff");
        _commentsAdded = meter.CreateCounter<long>(
            "reviews.comments_added", "{comment}", "Comments added to reviews");
        _operationDuration = meter.CreateHistogram<double>(
            "reviews.operation.duration", "ms", "Duration of review operations");

        meter.CreateObservableGauge("reviews.published", ObservePublishedReviews,
            "{review}", "Reviews currently published, by review type");
    }

    public void ReviewCreated(string reviewType, int rating)
        => _created.Add(1,
            ReviewType(reviewType),
            new KeyValuePair<string, object?>("review.rating", rating));

    public void ReviewReplied()
        => _replied.Add(1);

    public void CommentAdded(bool isStaff)
        => _commentsAdded.Add(1, new KeyValuePair<string, object?>("author.kind", isStaff ? "staff" : "customer"));

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

    public void UpdatePublishedReviews(IReadOnlyDictionary<string, long> countsByType)
        => Volatile.Write(ref _publishedReviews, countsByType);

    private IEnumerable<Measurement<long>> ObservePublishedReviews()
        => Volatile.Read(ref _publishedReviews).Select(pair => new Measurement<long>(pair.Value, ReviewType(pair.Key)));

    private static KeyValuePair<string, object?> ReviewType(string reviewType)
        => new("review.type", reviewType.ToLowerInvariant());
}

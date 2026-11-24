using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.ApiGateway.Logging;

public sealed class GatewayMetrics
{
    public const string MeterName = "Platform.ApiGateway";

    private readonly Counter<long> _requests;
    private readonly Counter<long> _errors;
    private readonly Histogram<double> _duration;

    public GatewayMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _requests = meter.CreateCounter<long>("gateway.requests", description: "Requests handled by the gateway");
        _errors = meter.CreateCounter<long>("gateway.errors", description: "Requests that ended with 5xx or a proxy error");
        _duration = meter.CreateHistogram<double>("gateway.request.duration", unit: "ms",
            description: "Time from receiving a request to sending the response");
    }

    public void Record(string route, string cluster, int statusCode, double elapsedMs, bool failed)
    {
        var tags = new TagList
        {
            { "route", route },
            { "cluster", cluster },
            { "status_code", statusCode }
        };

        _requests.Add(1, tags);
        _duration.Record(elapsedMs, tags);
        if (failed)
            _errors.Add(1, tags);
    }
}

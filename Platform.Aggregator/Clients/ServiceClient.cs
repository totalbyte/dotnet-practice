using System.Diagnostics;
using System.Net;

namespace Platform.Aggregator.Clients;

public abstract class ServiceClient(HttpClient http, ILogger logger)
{
    public abstract string ServiceName { get; }

    protected async Task<T?> GetOrDefaultAsync<T>(string uri, string target, CancellationToken cancellationToken)
        where T : class
    {
        logger.LogInformation("Calling {Service} for {Target}", ServiceName, target);
        var startedAt = Stopwatch.GetTimestamp();

        using var response = await http.GetAsync(uri, cancellationToken);

        logger.LogInformation("{Service} responded {StatusCode} for {Target} in {ElapsedMs} ms",
            ServiceName, (int)response.StatusCode, target, (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }
}

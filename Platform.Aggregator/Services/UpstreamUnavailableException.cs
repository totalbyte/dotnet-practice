namespace Platform.Aggregator.Services;

public sealed class UpstreamUnavailableException(string service, Exception innerException)
    : Exception($"{service} is unavailable", innerException)
{
    public string Service { get; } = service;
}

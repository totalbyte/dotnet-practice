using Polly.Timeout;

namespace Platform.Aggregator.Resilience;

// The gRPC client sees an attempt timeout as Internal and does not retry it. It reads an I/O error as
// Unavailable, so the RetryPolicy retries a hanging call (the aggregator only reads, retrying is safe).
// The handler sits outside the resilience pipeline.
public sealed class AttemptTimeoutHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new HttpRequestException("The attempt timed out", new IOException(ex.Message, ex));
        }
    }
}

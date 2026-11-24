using Grpc.Core;

namespace Catalog.Tests;

internal sealed class TestServerCallContext(string method, CancellationToken cancellationToken = default)
    : ServerCallContext
{
    protected override string MethodCore => method;
    protected override string HostCore => "localhost";
    protected override string PeerCore => "ipv4:127.0.0.1:50000";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore { get; } = [];
    protected override CancellationToken CancellationTokenCore => cancellationToken;
    protected override Metadata ResponseTrailersCore { get; } = [];
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore { get; } = new(null, []);

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
        => throw new NotSupportedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}

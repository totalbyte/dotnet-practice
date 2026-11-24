namespace Platform.Aggregator.Clients;

public static class UpstreamServices
{
    public const string Repairs = "repairs-api";
    public const string Catalog = "catalog-api";
    public const string Reviews = "reviews-api";

    // "_grpc." selects the HTTP/2-only endpoint named "grpc" of the Aspire resource through service discovery.
    public static Uri GrpcAddress(string service) => new($"http://_grpc.{service}");
}

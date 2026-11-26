namespace Platform.Aggregator.Clients;

public static class UpstreamServices
{
    public const string Repairs = "repairs-api";
    public const string Catalog = "catalog-api";
    public const string Reviews = "reviews-api";

    public static Uri GrpcAddress(string service) => new($"http://_grpc.{service}");
}

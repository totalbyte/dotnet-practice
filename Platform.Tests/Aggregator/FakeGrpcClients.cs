using Catalog.Grpc;
using Grpc.Core;
using Repairs.Grpc;
using Reviews.Grpc;

namespace Platform.Tests.Aggregator;

internal static class GrpcCalls
{
    public static AsyncUnaryCall<T> Success<T>(T reply) => FromTask(Task.FromResult(reply));

    public static AsyncUnaryCall<T> Failure<T>(StatusCode code, Exception? debugException = null)
        => FromTask(Task.FromException<T>(new RpcException(new Status(code, $"Simulated {code}", debugException))));

    public static AsyncUnaryCall<T> FromTask<T>(Task<T> response) => new(
        response, Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });
}

internal sealed class FakeRepairsClient(Func<GetRepairOrderRequest, CallOptions, AsyncUnaryCall<RepairOrderReply>> handler)
    : RepairsService.RepairsServiceClient
{
    public List<GetRepairOrderRequest> Requests { get; } = [];

    public override AsyncUnaryCall<RepairOrderReply> GetRepairOrderAsync(GetRepairOrderRequest request, CallOptions options)
    {
        Requests.Add(request);
        return handler(request, options);
    }
}

internal sealed class FakeCatalogClient(
    Func<GetCatalogItemsByIdsRequest, CallOptions, AsyncUnaryCall<CatalogItemsReply>> handler)
    : CatalogService.CatalogServiceClient
{
    public List<GetCatalogItemsByIdsRequest> Requests { get; } = [];

    public override AsyncUnaryCall<CatalogItemsReply> GetCatalogItemsByIdsAsync(
        GetCatalogItemsByIdsRequest request, CallOptions options)
    {
        Requests.Add(request);
        return handler(request, options);
    }
}

internal sealed class FakeReviewsClient(
    Func<GetRepairOrderReviewsRequest, CallOptions, AsyncUnaryCall<ReviewsReply>> handler)
    : ReviewsService.ReviewsServiceClient
{
    public List<GetRepairOrderReviewsRequest> Requests { get; } = [];

    public override AsyncUnaryCall<ReviewsReply> GetRepairOrderReviewsAsync(
        GetRepairOrderReviewsRequest request, CallOptions options)
    {
        Requests.Add(request);
        return handler(request, options);
    }
}

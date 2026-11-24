using System.Diagnostics;
using Catalog.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Dtos;
using Polly;
using Repairs.Grpc;
using Reviews.Grpc;

namespace Platform.Aggregator.Services;

public sealed class RepairOrderCardService(
    RepairsService.RepairsServiceClient repairs,
    CatalogService.CatalogServiceClient catalog,
    ReviewsService.ReviewsServiceClient reviews,
    RepairOrderCardCache cache,
    IOptions<AggregatorOptions> options,
    ILogger<RepairOrderCardService> logger)
{
    public static readonly ActivitySource ActivitySource = new("Platform.Aggregator");

    public async Task<RepairOrderCardDto?> GetAsync(long repairOrderId, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("AggregateRepairOrderCard");
        activity?.SetTag("repair_order.id", repairOrderId);

        if (await cache.GetAsync(repairOrderId, cancellationToken) is { } cached)
        {
            activity?.SetTag("aggregator.cache", "hit");
            logger.LogInformation("Card of repair order {RepairOrderId} served from cache", repairOrderId);
            return cached;
        }

        activity?.SetTag("aggregator.cache", "miss");
        var card = await BuildAsync(repairOrderId, activity, cancellationToken);
        if (card is not null)
            await cache.SetAsync(card, cancellationToken);

        return card;
    }

    private async Task<RepairOrderCardDto?> BuildAsync(
        long repairOrderId, Activity? activity, CancellationToken cancellationToken)
    {
        var order = await GetRepairOrderAsync(repairOrderId, cancellationToken);
        if (order is null)
            return null;

        var itemIds = order.Items.Select(item => item.CatalogItemId).Distinct().ToList();
        var warnings = new List<string>();
        var settings = options.Value;

        var catalogTask = CallSafelyAsync(UpstreamServices.Catalog, async () => itemIds.Count == 0
                ? []
                : (await catalog.GetCatalogItemsByIdsAsync(
                    new GetCatalogItemsByIdsRequest { Ids = { itemIds } },
                    deadline: Deadline(), cancellationToken: cancellationToken)).Items.ToList(),
            warnings, cancellationToken);
        var reviewsTask = CallSafelyAsync(UpstreamServices.Reviews, async () => (await reviews.GetRepairOrderReviewsAsync(
                new GetRepairOrderReviewsRequest
                {
                    RepairOrderId = order.Id,
                    CatalogItemIds = { itemIds },
                    RepairReviewsLimit = settings.RepairReviewsLimit,
                    ItemReviewsLimit = settings.ItemReviewsLimit
                },
                deadline: Deadline(), cancellationToken: cancellationToken)).Reviews.ToList(),
            warnings, cancellationToken);

        await Task.WhenAll(catalogTask, reviewsTask);

        var catalogItems = await catalogTask;
        var orderReviews = await reviewsTask;

        var itemsById = catalogItems?
            .Where(item => itemIds.Contains(item.Id))
            .ToDictionary(item => item.Id);

        if (itemsById is not null)
            warnings.AddRange(itemIds
                .Where(id => !itemsById.ContainsKey(id))
                .Select(id => $"Catalog item {id} was not found in {UpstreamServices.Catalog}"));

        var card = new RepairOrderCardDto(
            ToOrderInfo(order),
            [.. order.Items.Select(item => ToPart(item, itemsById?.GetValueOrDefault(item.CatalogItemId)))],
            [.. (orderReviews ?? [])
                .Where(review => BelongsToOrder(review, order.Id, itemIds))
                .OrderByDescending(review => review.CreatedAt.ToDateTime())
                .Select(ToReviewInfo)],
            warnings);

        activity?.SetTag("aggregator.warnings", warnings.Count);
        logger.LogInformation(
            "Card of repair order {RepairOrderId} built with {PartsCount} parts, {ReviewsCount} reviews and {WarningsCount} warnings",
            order.Id, card.Parts.Count, card.Reviews.Count, warnings.Count);

        return card;

        DateTime Deadline() => DateTime.UtcNow.Add(settings.CallDeadline);
    }

    private async Task<RepairOrderReply?> GetRepairOrderAsync(long id, CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            var order = await repairs.GetRepairOrderAsync(new GetRepairOrderRequest { Id = id },
                deadline: DateTime.UtcNow.Add(options.Value.CallDeadline), cancellationToken: cancellationToken);
            logger.LogInformation("{Service} responded in {ElapsedMs} ms", UpstreamServices.Repairs, ElapsedMs(startedAt));
            return order;
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound or StatusCode.InvalidArgument)
        {
            logger.LogInformation("{Service} answered {GrpcStatus} for repair order {RepairOrderId} in {ElapsedMs} ms",
                UpstreamServices.Repairs, ex.StatusCode, id, ElapsedMs(startedAt));
            return null;
        }
        catch (RpcException ex) when (IsUnavailable(ex, cancellationToken))
        {
            logger.LogError(ex,
                "{Service} is unavailable ({GrpcStatus}) after {ElapsedMs} ms, repair order {RepairOrderId} cannot be aggregated",
                UpstreamServices.Repairs, ex.StatusCode, ElapsedMs(startedAt), id);
            throw new UpstreamUnavailableException(UpstreamServices.Repairs, ex);
        }
    }

    private async Task<T?> CallSafelyAsync<T>(
        string service, Func<Task<T>> call, List<string> warnings, CancellationToken cancellationToken)
        where T : class
    {
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            var result = await call();
            logger.LogInformation("{Service} responded in {ElapsedMs} ms", service, ElapsedMs(startedAt));
            return result;
        }
        catch (RpcException ex) when (IsUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "{Service} is unavailable ({GrpcStatus}) after {ElapsedMs} ms",
                service, ex.StatusCode, ElapsedMs(startedAt));
            lock (warnings)
                warnings.Add($"{service} is unavailable");
            return null;
        }
    }

    // Unavailable and DeadlineExceeded are the transport failures of gRPC; a rejection by the
    // HTTP resilience pipeline (open circuit, exhausted timeout) reaches the client as Internal.
    private static bool IsUnavailable(RpcException exception, CancellationToken cancellationToken)
        => exception.StatusCode switch
        {
            StatusCode.Unavailable or StatusCode.DeadlineExceeded => true,
            StatusCode.Internal => exception.Status.DebugException is ExecutionRejectedException or HttpRequestException,
            StatusCode.Cancelled => !cancellationToken.IsCancellationRequested,
            _ => false
        };

    private static bool BelongsToOrder(ReviewReply review, long orderId, List<long> itemIds)
        => review.HasRepairOrderId && review.RepairOrderId == orderId
           || review.HasCatalogItemId && itemIds.Contains(review.CatalogItemId);

    private static RepairOrderInfoDto ToOrderInfo(RepairOrderReply order) => new(
        order.Id,
        order.CustomerId,
        order.Status.ToString(),
        order.AcceptedAt.ToDateTime(),
        order.IssuedAt?.ToDateTime(),
        order.Device is { } device
            ? new DeviceInfoDto(device.DeviceType, device.HasDeviceBrand ? device.DeviceBrand : null,
                device.DeviceModel, device.DefectDescription, device.HasDiagnosticNotes ? device.DiagnosticNotes : null)
            : null,
        order.TotalAmount.ToDecimal());

    private static RepairOrderPartDto ToPart(RepairOrderItem item, CatalogItemReply? catalogItem)
    {
        var unitPrice = item.UnitPrice.ToDecimal();
        return new RepairOrderPartDto(
            item.CatalogItemId,
            item.ItemName,
            item.Quantity,
            unitPrice,
            item.Discount.ToDecimal(),
            item.LineTotal.ToDecimal(),
            catalogItem is null
                ? null
                : new CatalogItemInfoDto(
                    catalogItem.Sku,
                    catalogItem.Name,
                    catalogItem.ItemType.ToString(),
                    catalogItem.HasBrandName ? catalogItem.BrandName : null,
                    catalogItem.Price.ToDecimal(),
                    catalogItem.Price.ToDecimal() != unitPrice,
                    catalogItem.IsActive,
                    catalogItem.WarrantyMonths));
    }

    private static ReviewInfoDto ToReviewInfo(ReviewReply review) => new(
        review.Id,
        review.ReviewType.ToString().ToLowerInvariant(),
        review.Rating,
        review.HasTitle ? review.Title : null,
        review.Text,
        review.Author?.DisplayName,
        review.HasCatalogItemId ? review.CatalogItemId : null,
        review.CreatedAt.ToDateTime());

    private static long ElapsedMs(long startedAt) => (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
}

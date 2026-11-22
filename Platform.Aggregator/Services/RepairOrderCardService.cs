using System.Diagnostics;
using System.Text.Json;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Clients.Contracts;
using Platform.Aggregator.Dtos;
using Polly;

namespace Platform.Aggregator.Services;

public sealed class RepairOrderCardService(
    RepairsClient repairs,
    CatalogClient catalog,
    ReviewsClient reviews,
    ILogger<RepairOrderCardService> logger)
{
    public static readonly ActivitySource ActivitySource = new("Platform.Aggregator");

    public async Task<RepairOrderCardDto?> GetAsync(long repairOrderId, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("AggregateRepairOrderCard");
        activity?.SetTag("repair_order.id", repairOrderId);

        var order = await GetRepairOrderAsync(repairOrderId, cancellationToken);
        if (order is null)
            return null;

        var itemIds = order.Items.Select(item => item.CatalogItemId).Distinct().ToList();
        var warnings = new List<string>();

        var catalogTask = CallSafelyAsync(catalog.ServiceName,
            () => catalog.GetItemsAsync(itemIds, cancellationToken), warnings, cancellationToken);
        var reviewsTask = CallSafelyAsync(reviews.ServiceName,
            () => reviews.GetForRepairOrderAsync(order.Id, itemIds, cancellationToken), warnings, cancellationToken);

        await Task.WhenAll(catalogTask, reviewsTask);

        var catalogItems = await catalogTask;
        var orderReviews = await reviewsTask;

        var itemsById = catalogItems?
            .Where(item => itemIds.Contains(item.Id))
            .ToDictionary(item => item.Id);

        if (itemsById is not null)
            warnings.AddRange(itemIds
                .Where(id => !itemsById.ContainsKey(id))
                .Select(id => $"Catalog item {id} was not found in {catalog.ServiceName}"));

        var card = new RepairOrderCardDto(
            ToOrderInfo(order),
            [.. order.Items.Select(item => ToPart(item, itemsById?.GetValueOrDefault(item.CatalogItemId)))],
            [.. (orderReviews ?? [])
                .Where(review => BelongsToOrder(review, order.Id, itemIds))
                .OrderByDescending(review => review.CreatedAt)
                .Select(ToReviewInfo)],
            warnings);

        activity?.SetTag("aggregator.warnings", warnings.Count);
        logger.LogInformation(
            "Card of repair order {RepairOrderId} built with {PartsCount} parts, {ReviewsCount} reviews and {WarningsCount} warnings",
            order.Id, card.Parts.Count, card.Reviews.Count, warnings.Count);

        return card;
    }

    private async Task<RepairOrderResponse?> GetRepairOrderAsync(long id, CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            var order = await repairs.GetRepairOrderAsync(id, cancellationToken);
            logger.LogInformation("{Service} responded in {ElapsedMs} ms",
                repairs.ServiceName, ElapsedMs(startedAt));
            return order;
        }
        catch (Exception ex) when (IsUnavailable(ex, cancellationToken))
        {
            logger.LogError(ex, "{Service} is unavailable after {ElapsedMs} ms, repair order {RepairOrderId} cannot be aggregated",
                repairs.ServiceName, ElapsedMs(startedAt), id);
            throw new UpstreamUnavailableException(repairs.ServiceName, ex);
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
        catch (Exception ex) when (IsUnavailable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "{Service} is unavailable after {ElapsedMs} ms: {ErrorType}",
                service, ElapsedMs(startedAt), ex.GetType().Name);
            lock (warnings)
                warnings.Add($"{service} is unavailable");
            return null;
        }
    }

    private static bool IsUnavailable(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        HttpRequestException or ExecutionRejectedException or JsonException => true,
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false
    };

    private static bool BelongsToOrder(ReviewResponse review, long orderId, List<long> itemIds)
        => review.RepairOrderId == orderId
           || review.CatalogItemId is { } itemId && itemIds.Contains(itemId);

    private static RepairOrderInfoDto ToOrderInfo(RepairOrderResponse order) => new(
        order.Id,
        order.CustomerId,
        order.Status,
        order.AcceptedAt,
        order.IssuedAt,
        order.Device is { } device
            ? new DeviceInfoDto(device.DeviceType, device.DeviceBrand, device.DeviceModel,
                device.DefectDescription, device.DiagnosticNotes)
            : null,
        order.TotalAmount);

    private static RepairOrderPartDto ToPart(RepairOrderItemResponse item, CatalogItemResponse? catalogItem) => new(
        item.CatalogItemId,
        item.ItemName,
        item.Quantity,
        item.UnitPrice,
        item.Discount,
        item.LineTotal,
        catalogItem is null
            ? null
            : new CatalogItemInfoDto(
                catalogItem.Sku,
                catalogItem.Name,
                catalogItem.ItemType,
                catalogItem.BrandName,
                catalogItem.Price,
                catalogItem.Price != item.UnitPrice,
                catalogItem.IsActive,
                catalogItem.WarrantyMonths));

    private static ReviewInfoDto ToReviewInfo(ReviewResponse review) => new(
        review.Id,
        review.ReviewType,
        review.Rating,
        review.Title,
        review.Text,
        review.Author?.DisplayName,
        review.CatalogItemId,
        review.CreatedAt);

    private static long ElapsedMs(long startedAt) => (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
}

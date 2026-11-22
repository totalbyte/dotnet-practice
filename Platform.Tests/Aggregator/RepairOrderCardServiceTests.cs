using System.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Clients.Contracts;
using Platform.Aggregator.Services;
using Polly.Timeout;

namespace Platform.Tests.Aggregator;

public sealed class RepairOrderCardServiceTests
{
    private const long OrderId = 7;

    private static readonly RepairOrderResponse Order = new()
    {
        Id = OrderId,
        CustomerId = 3,
        Status = "InRepair",
        AcceptedAt = new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc),
        Device = new RepairOrderDeviceResponse
        {
            DeviceType = "Smartphone", DeviceBrand = "Apple", DeviceModel = "iPhone 13", DefectDescription = "Broken screen"
        },
        Items =
        [
            new RepairOrderItemResponse { CatalogItemId = 1, ItemName = "Screen", UnitPrice = 100m, Quantity = 1, LineTotal = 100m },
            new RepairOrderItemResponse { CatalogItemId = 2, ItemName = "Labour", UnitPrice = 50m, Quantity = 1, LineTotal = 50m }
        ],
        TotalAmount = 150m
    };

    private StubHttpHandler _repairs = StubHttpHandler.Json(_ => Order);
    private StubHttpHandler _catalog = StubHttpHandler.Json(request => CatalogItem(IdFromPath(request)));
    private StubHttpHandler _reviews = StubHttpHandler.Json(ReviewsPage);

    [Fact]
    public async Task Builds_full_card_from_all_services()
    {
        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Empty(card.Warnings);
        Assert.Equal(OrderId, card.Order.Id);
        Assert.Equal("iPhone 13", card.Order.Device!.Model);
        Assert.All(card.Parts, part => Assert.NotNull(part.Catalog));
        Assert.True(card.Parts.Single(part => part.CatalogItemId == 1).Catalog!.PriceChanged);
        Assert.False(card.Parts.Single(part => part.CatalogItemId == 2).Catalog!.PriceChanged);
        Assert.Equal(["r-item-1", "r-item-2", "r-order"], card.Reviews.Select(review => review.Id).Order());
    }

    [Fact]
    public async Task Returns_order_and_parts_with_warning_when_reviews_api_is_unavailable()
    {
        _reviews = StubHttpHandler.Failing(new HttpRequestException("Connection refused"));

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Equal(["reviews-api is unavailable"], card.Warnings);
        Assert.Empty(card.Reviews);
        Assert.All(card.Parts, part => Assert.NotNull(part.Catalog));
    }

    [Fact]
    public async Task Returns_order_without_catalog_data_when_catalog_api_times_out()
    {
        _catalog = StubHttpHandler.Failing(new TimeoutRejectedException());

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Equal(["catalog-api is unavailable"], card.Warnings);
        Assert.All(card.Parts, part => Assert.Null(part.Catalog));
        Assert.NotEmpty(card.Reviews);
    }

    [Fact]
    public async Task Returns_null_when_repair_order_does_not_exist()
    {
        _repairs = StubHttpHandler.Json(_ => null);

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.Null(card);
        Assert.Empty(_catalog.Requests);
        Assert.Empty(_reviews.Requests);
    }

    [Fact]
    public async Task Throws_when_repairs_api_is_unavailable()
    {
        _repairs = StubHttpHandler.Failing(new HttpRequestException("Connection refused"));

        var exception = await Assert.ThrowsAsync<UpstreamUnavailableException>(
            () => CreateService().GetAsync(OrderId, CancellationToken.None));

        Assert.Equal("repairs-api", exception.Service);
    }

    [Fact]
    public async Task Keeps_only_data_that_belongs_to_the_order()
    {
        _catalog = StubHttpHandler.Json(request => IdFromPath(request) == 2 ? CatalogItem(99) : CatalogItem(1));
        _reviews = StubHttpHandler.Json(_ => new ReviewPageResponse(
        [
            Review("r-order", repairOrderId: OrderId),
            Review("r-foreign-order", repairOrderId: 8),
            Review("r-foreign-item", catalogItemId: 5)
        ], 3));

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Null(card.Parts.Single(part => part.CatalogItemId == 2).Catalog);
        Assert.Equal(["Catalog item 2 was not found in catalog-api"], card.Warnings);
        Assert.Equal(["r-order"], card.Reviews.Select(review => review.Id));
    }

    [Fact]
    public async Task Queries_catalog_and_reviews_in_parallel()
    {
        var catalogStarted = new TaskCompletionSource();
        var reviewsStarted = new TaskCompletionSource();
        _catalog = new StubHttpHandler(async (request, _) =>
        {
            catalogStarted.TrySetResult();
            await reviewsStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return await StubHttpHandler.Json(r => CatalogItem(IdFromPath(r))).InvokeAsync(request);
        });
        _reviews = new StubHttpHandler(async (request, _) =>
        {
            reviewsStarted.TrySetResult();
            await catalogStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return await StubHttpHandler.Json(ReviewsPage).InvokeAsync(request);
        });

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Empty(card.Warnings);
    }

    [Fact]
    public async Task Propagates_cancellation_requested_by_the_client()
    {
        using var cancellation = new CancellationTokenSource();
        _catalog = new StubHttpHandler(async (_, token) =>
        {
            await cancellation.CancelAsync();
            await Task.Delay(Timeout.Infinite, token);
            throw new InvalidOperationException("Unreachable");
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateService().GetAsync(OrderId, cancellation.Token));
    }

    private RepairOrderCardService CreateService() => new(
        new RepairsClient(Client(_repairs, "repairs-api"), NullLogger<RepairsClient>.Instance),
        new CatalogClient(Client(_catalog, "catalog-api"), NullLogger<CatalogClient>.Instance),
        new ReviewsClient(Client(_reviews, "reviews-api"), NullLogger<ReviewsClient>.Instance),
        NullLogger<RepairOrderCardService>.Instance);

    private static HttpClient Client(HttpMessageHandler handler, string service)
        => new(handler) { BaseAddress = new Uri($"http://{service}") };

    private static long IdFromPath(HttpRequestMessage request) => long.Parse(request.RequestUri!.Segments[^1]);

    private static CatalogItemResponse CatalogItem(long id) => new()
    {
        Id = id,
        Sku = $"SKU-{id}",
        Name = $"Item {id}",
        ItemType = id == 2 ? "Service" : "Part",
        Price = id == 1 ? 120m : 50m,
        IsActive = true
    };

    private static ReviewPageResponse ReviewsPage(HttpRequestMessage request)
    {
        var query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
        return query["repairOrderId"] is { } orderId
            ? new ReviewPageResponse([Review("r-order", repairOrderId: long.Parse(orderId))], 1)
            : new ReviewPageResponse([Review($"r-item-{query["catalogItemId"]}", catalogItemId: long.Parse(query["catalogItemId"]!))], 1);
    }

    private static ReviewResponse Review(string id, long? repairOrderId = null, long? catalogItemId = null) => new()
    {
        Id = id,
        ReviewType = repairOrderId is null ? "item" : "repair",
        Rating = 5,
        Text = "Good",
        Author = new ReviewAuthorResponse(3, "Olena"),
        RepairOrderId = repairOrderId,
        CatalogItemId = catalogItemId,
        CreatedAt = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc)
    };
}

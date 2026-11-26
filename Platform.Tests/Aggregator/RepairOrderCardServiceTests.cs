using System.Diagnostics.Metrics;
using Catalog.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Services;
using Polly.Timeout;
using Repairs.Grpc;
using Reviews.Grpc;
using CatalogDecimal = Catalog.Grpc.DecimalValue;
using RepairsDecimal = Repairs.Grpc.DecimalValue;

namespace Platform.Tests.Aggregator;

public sealed class RepairOrderCardServiceTests
{
    private const long OrderId = 7;

    private static readonly DateTime CreatedAt = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly IDistributedCache _redis =
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private FakeRepairsClient _repairs = new((_, _) => GrpcCalls.Success(Order()));
    private FakeCatalogClient _catalog = new((request, _) => GrpcCalls.Success(CatalogItems(request.Ids)));
    private FakeReviewsClient _reviews = new((request, _) => GrpcCalls.Success(ReviewsFor(request)));

    [Fact]
    public async Task Builds_full_card_with_one_call_per_service()
    {
        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Empty(card.Warnings);
        Assert.Equal(OrderId, card.Order.Id);
        Assert.Equal("InRepair", card.Order.Status);
        Assert.Equal("iPhone 13", card.Order.Device!.Model);
        Assert.Equal(150m, card.Order.TotalAmount);
        Assert.All(card.Parts, part => Assert.NotNull(part.Catalog));
        Assert.True(card.Parts.Single(part => part.CatalogItemId == 1).Catalog!.PriceChanged);
        Assert.False(card.Parts.Single(part => part.CatalogItemId == 2).Catalog!.PriceChanged);
        Assert.Equal(["r-item-1", "r-item-2", "r-order"], card.Reviews.Select(review => review.Id).Order());
        Assert.Equal([1L, 2L], Assert.Single(_catalog.Requests).Ids);
        Assert.Equal([1L, 2L], Assert.Single(_reviews.Requests).CatalogItemIds);
    }

    [Fact]
    public async Task Passes_deadline_and_cancellation_token_to_every_call()
    {
        using var cancellation = new CancellationTokenSource();
        var options = new List<CallOptions>();
        _repairs = new((_, o) => { options.Add(o); return GrpcCalls.Success(Order()); });
        _catalog = new((request, o) => { options.Add(o); return GrpcCalls.Success(CatalogItems(request.Ids)); });
        _reviews = new((request, o) => { options.Add(o); return GrpcCalls.Success(ReviewsFor(request)); });

        await CreateService().GetAsync(OrderId, cancellation.Token);

        Assert.Equal(3, options.Count);
        Assert.All(options, o =>
        {
            Assert.Equal(cancellation.Token, o.CancellationToken);
            Assert.NotNull(o.Deadline);
        });
    }

    [Fact]
    public async Task Returns_order_and_parts_with_warning_when_reviews_api_is_unavailable()
    {
        _reviews = new((_, _) => GrpcCalls.Failure<ReviewsReply>(StatusCode.Unavailable));

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Equal(["reviews-api is unavailable"], card.Warnings);
        Assert.Empty(card.Reviews);
        Assert.All(card.Parts, part => Assert.NotNull(part.Catalog));
    }

    [Fact]
    public async Task Returns_order_without_catalog_data_when_catalog_api_misses_the_deadline()
    {
        _catalog = new((_, _) => GrpcCalls.Failure<CatalogItemsReply>(StatusCode.DeadlineExceeded));

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Equal(["catalog-api is unavailable"], card.Warnings);
        Assert.All(card.Parts, part => Assert.Null(part.Catalog));
        Assert.NotEmpty(card.Reviews);
    }

    [Fact]
    public async Task Treats_resilience_timeout_as_unavailable_service()
    {
        _catalog = new((_, _) => GrpcCalls.Failure<CatalogItemsReply>(StatusCode.Internal, new TimeoutRejectedException()));

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Equal(["catalog-api is unavailable"], card.Warnings);
    }

    [Theory]
    [InlineData(StatusCode.Unknown)]
    [InlineData(StatusCode.ResourceExhausted)]
    public async Task Returns_partial_card_when_a_service_fails_to_handle_the_call(StatusCode status)
    {
        _catalog = new((_, _) => GrpcCalls.Failure<CatalogItemsReply>(status));
        _reviews = new((_, _) => GrpcCalls.Failure<ReviewsReply>(status));

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Equal(["catalog-api is unavailable", "reviews-api is unavailable"], card.Warnings.Order());
        Assert.Equal(2, card.Parts.Count);
    }

    [Theory]
    [InlineData(StatusCode.Unknown)]
    [InlineData(StatusCode.ResourceExhausted)]
    public async Task Throws_unavailable_when_repairs_api_fails_to_handle_the_call(StatusCode status)
    {
        _repairs = new((_, _) => GrpcCalls.Failure<RepairOrderReply>(status));

        var exception = await Assert.ThrowsAsync<UpstreamUnavailableException>(
            () => CreateService().GetAsync(OrderId, CancellationToken.None));

        Assert.Equal("repairs-api", exception.Service);
    }

    [Fact]
    public async Task Returns_null_when_repair_order_does_not_exist()
    {
        _repairs = new((_, _) => GrpcCalls.Failure<RepairOrderReply>(StatusCode.NotFound));

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.Null(card);
        Assert.Empty(_catalog.Requests);
        Assert.Empty(_reviews.Requests);
    }

    [Fact]
    public async Task Throws_when_repairs_api_is_unavailable()
    {
        _repairs = new((_, _) => GrpcCalls.Failure<RepairOrderReply>(StatusCode.Unavailable));

        var exception = await Assert.ThrowsAsync<UpstreamUnavailableException>(
            () => CreateService().GetAsync(OrderId, CancellationToken.None));

        Assert.Equal("repairs-api", exception.Service);
    }

    [Fact]
    public async Task Keeps_only_data_that_belongs_to_the_order()
    {
        _catalog = new((_, _) => GrpcCalls.Success(new CatalogItemsReply { Items = { CatalogItem(1), CatalogItem(99) } }));
        _reviews = new((_, _) => GrpcCalls.Success(new ReviewsReply
        {
            Reviews =
            {
                Review("r-order", repairOrderId: OrderId),
                Review("r-foreign-order", repairOrderId: 8),
                Review("r-foreign-item", catalogItemId: 5)
            }
        }));

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Null(card.Parts.Single(part => part.CatalogItemId == 2).Catalog);
        Assert.Equal(["Catalog item 2 was not found in catalog-api"], card.Warnings);
        Assert.Equal(["r-order"], card.Reviews.Select(review => review.Id));
    }

    [Fact]
    public async Task Calls_catalog_and_reviews_in_parallel()
    {
        var catalogStarted = new TaskCompletionSource();
        var reviewsStarted = new TaskCompletionSource();
        _catalog = new((request, _) => GrpcCalls.FromTask(Task.Run(async () =>
        {
            catalogStarted.TrySetResult();
            await reviewsStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return CatalogItems(request.Ids);
        })));
        _reviews = new((request, _) => GrpcCalls.FromTask(Task.Run(async () =>
        {
            reviewsStarted.TrySetResult();
            await catalogStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return ReviewsFor(request);
        })));

        var card = await CreateService().GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Empty(card.Warnings);
    }

    [Fact]
    public async Task Propagates_cancellation_requested_by_the_client()
    {
        using var cancellation = new CancellationTokenSource();
        _catalog = new((_, _) =>
        {
            cancellation.Cancel();
            return GrpcCalls.Failure<CatalogItemsReply>(StatusCode.Cancelled);
        });

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => CreateService().GetAsync(OrderId, cancellation.Token));

        Assert.Equal(StatusCode.Cancelled, exception.StatusCode);
    }

    [Fact]
    public async Task Serves_repeated_request_from_redis_without_grpc_calls()
    {
        var service = CreateService();

        await service.GetAsync(OrderId, CancellationToken.None);
        var cached = await service.GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(cached);
        Assert.Equal(2, cached.Parts.Count);
        Assert.Single(_repairs.Requests);
        Assert.Single(_catalog.Requests);
        Assert.Single(_reviews.Requests);
        Assert.NotNull(await _redis.GetAsync(RepairOrderCardCache.Key(OrderId)));
    }

    [Fact]
    public async Task Does_not_cache_partial_card()
    {
        _reviews = new((_, _) => GrpcCalls.Failure<ReviewsReply>(StatusCode.Unavailable));
        var service = CreateService();

        await service.GetAsync(OrderId, CancellationToken.None);
        await service.GetAsync(OrderId, CancellationToken.None);

        Assert.Equal(2, _repairs.Requests.Count);
        Assert.Null(await _redis.GetAsync(RepairOrderCardCache.Key(OrderId)));
    }

    [Fact]
    public async Task Builds_card_when_redis_is_down()
    {
        var card = await CreateService(new FailingDistributedCache()).GetAsync(OrderId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Empty(card.Warnings);
    }

    private RepairOrderCardService CreateService(IDistributedCache? redis = null)
    {
        var options = Options.Create(new AggregatorOptions());
        var cache = new RepairOrderCardCache(redis ?? _redis, new AggregatorCacheMetrics(new TestMeterFactory()),
            options, TimeProvider.System, NullLogger<RepairOrderCardCache>.Instance);
        return new RepairOrderCardService(
            _repairs, _catalog, _reviews, cache, options, TimeProvider.System, NullLogger<RepairOrderCardService>.Instance);
    }

    private static RepairOrderReply Order() => new()
    {
        Id = OrderId,
        CustomerId = 3,
        Status = RepairOrderStatus.InRepair,
        AcceptedAt = Timestamp.FromDateTime(new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc)),
        Device = new Device
        {
            DeviceType = "Smartphone", DeviceBrand = "Apple", DeviceModel = "iPhone 13", DefectDescription = "Broken screen"
        },
        Items =
        {
            new RepairOrderItem
            {
                CatalogItemId = 1, ItemName = "Screen", UnitPrice = RepairsMoney(100), Quantity = 1,
                Discount = RepairsMoney(0), LineTotal = RepairsMoney(100)
            },
            new RepairOrderItem
            {
                CatalogItemId = 2, ItemName = "Labour", UnitPrice = RepairsMoney(50), Quantity = 1,
                Discount = RepairsMoney(0), LineTotal = RepairsMoney(50)
            }
        },
        TotalAmount = RepairsMoney(150)
    };

    private static CatalogItemsReply CatalogItems(IEnumerable<long> ids)
        => new() { Items = { ids.Select(CatalogItem) } };

    private static CatalogItemReply CatalogItem(long id) => new()
    {
        Id = id,
        Sku = $"SKU-{id}",
        Name = $"Item {id}",
        ItemType = id == 2 ? CatalogItemType.Service : CatalogItemType.Part,
        Price = new CatalogDecimal { Units = id == 1 ? 120 : 50 },
        IsActive = true
    };

    private static ReviewsReply ReviewsFor(GetRepairOrderReviewsRequest request) => new()
    {
        Reviews =
        {
            request.CatalogItemIds.Select(id => Review($"r-item-{id}", catalogItemId: id))
                .Prepend(Review("r-order", repairOrderId: request.RepairOrderId))
        }
    };

    private static ReviewReply Review(string id, long? repairOrderId = null, long? catalogItemId = null)
    {
        var review = new ReviewReply
        {
            Id = id,
            ReviewType = repairOrderId is null ? ReviewType.Item : ReviewType.Repair,
            Rating = 5,
            Text = "Good",
            Author = new Author { CustomerId = 3, DisplayName = "Olena" },
            CreatedAt = Timestamp.FromDateTime(CreatedAt)
        };
        if (repairOrderId is { } orderId)
            review.RepairOrderId = orderId;
        if (catalogItemId is { } itemId)
            review.CatalogItemId = itemId;
        return review;
    }

    private static RepairsDecimal RepairsMoney(long units) => new() { Units = units };

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);

        public void Dispose()
        {
        }
    }

    private sealed class FailingDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw new TimeoutException("redis down");

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            => Task.FromException<byte[]?>(new TimeoutException("redis down"));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            => throw new TimeoutException("redis down");

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
            => Task.FromException(new TimeoutException("redis down"));

        public void Refresh(string key) => throw new TimeoutException("redis down");

        public Task RefreshAsync(string key, CancellationToken token = default)
            => Task.FromException(new TimeoutException("redis down"));

        public void Remove(string key) => throw new TimeoutException("redis down");

        public Task RemoveAsync(string key, CancellationToken token = default)
            => Task.FromException(new TimeoutException("redis down"));
    }
}

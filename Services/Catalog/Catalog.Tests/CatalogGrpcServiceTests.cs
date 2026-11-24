using Catalog.Api.Grpc;
using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Dtos.Categories;
using Catalog.Bll.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Grpc;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Catalog.Tests;

public class CatalogGrpcServiceTests
{
    private readonly ICatalogItemService _items = Substitute.For<ICatalogItemService>();
    private readonly ICategoryService _categories = Substitute.For<ICategoryService>();
    private readonly CatalogGrpcService _service;
    private readonly ServerCallContext _context = new TestServerCallContext("/catalog.CatalogService/GetCatalogItem");

    public CatalogGrpcServiceTests() => _service = new CatalogGrpcService(_items, _categories);

    [Fact]
    public async Task GetCatalogItem_NonPositiveId_ReturnsInvalidArgumentWithoutCallingBll()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => _service.GetCatalogItem(new GetCatalogItemRequest { Id = 0 }, _context));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        await _items.DidNotReceiveWithAnyArgs().GetByIdAsync(default);
    }

    [Fact]
    public async Task GetCatalogItem_MissingItem_ReturnsNotFound()
    {
        _items.GetByIdAsync(99, Arg.Any<CancellationToken>()).ThrowsAsync(new EntityNotFoundException("CatalogItem", 99));

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => _service.GetCatalogItem(new GetCatalogItemRequest { Id = 99 }, _context));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
        Assert.Equal("CatalogItem '99' was not found", exception.Status.Detail);
    }

    [Fact]
    public async Task GetCatalogItem_ExistingItem_MapsExactPriceAndOptionalFields()
    {
        _items.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new CatalogItemDetailsDto
        {
            Id = 1, Sku = "PRT-DSP-IP13", Name = "Screen", ItemType = "Part", Price = 4200.55m,
            BrandId = 1, BrandName = "Apple", IsActive = true, WarrantyMonths = 6,
            CreatedAt = new DateTime(2026, 10, 1, 9, 0, 0)
        });

        var reply = await _service.GetCatalogItem(new GetCatalogItemRequest { Id = 1 }, _context);

        Assert.Equal(CatalogItemType.Part, reply.ItemType);
        Assert.Equal(4200, reply.Price.Units);
        Assert.Equal(550_000_000, reply.Price.Nanos);
        Assert.Equal("Apple", reply.BrandName);
        Assert.False(reply.HasEstimatedDurationMinutes);
        Assert.Null(reply.UpdatedAt);
    }

    [Fact]
    public async Task GetCatalogItemsByIds_TooManyIds_ReturnsInvalidArgument()
    {
        var request = new GetCatalogItemsByIdsRequest { Ids = { Enumerable.Range(1, 101).Select(id => (long)id) } };

        var exception = await Assert.ThrowsAsync<RpcException>(() => _service.GetCatalogItemsByIds(request, _context));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task GetCatalogItemsByIds_PassesIdsToBll()
    {
        _items.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([new CatalogItemDetailsDto { Id = 1, ItemType = "Service" }]);

        var reply = await _service.GetCatalogItemsByIds(new GetCatalogItemsByIdsRequest { Ids = { 1, 2 } }, _context);

        Assert.Equal(CatalogItemType.Service, Assert.Single(reply.Items).ItemType);
        await _items.Received(1).GetByIdsAsync(
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new long[] { 1, 2 })), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListCategories_MapsParentCategory()
    {
        _categories.GetListAsync(Arg.Any<CategoryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([new CategoryDto(2, "Screens", "screens", 1, 0)], 1, 1, 100));

        var reply = await _service.ListCategories(new ListCategoriesRequest(), _context);

        var category = Assert.Single(reply.Categories);
        Assert.Equal(1, category.ParentCategoryId);
    }
}

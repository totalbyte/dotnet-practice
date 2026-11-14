using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Repairs.Bll.Dtos.RepairOrders;
using Repairs.Bll.Services;
using Repairs.Dal.Interfaces;
using Repairs.Domain.Constants;
using Repairs.Domain.Exceptions;
using Repairs.Domain.Models;

namespace Repairs.Tests;

public class RepairOrderServiceTests
{
    private const long CustomerId = 1;
    private const long OrderId = 42;

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly RepairOrderService _service;

    public RepairOrderServiceTests()
    {
        _service = new RepairOrderService(_uow, TestMapper.Create(), NullLogger<RepairOrderService>.Instance);

        _uow.Customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>())
            .Returns(new Customer { Id = CustomerId });
        _uow.RepairOrders.AddAsync(Arg.Any<RepairOrder>(), Arg.Any<CancellationToken>())
            .Returns(OrderId);
        _uow.RepairOrders.GetWithItemsAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(new RepairOrder { Id = OrderId, CustomerId = CustomerId });
        _uow.RepairOrders.GetStatusHistoryAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public async Task ChangeStatusAsync_OrderDoesNotExist_ThrowsNotFound()
    {
        _uow.RepairOrders.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((RepairOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.ChangeStatusAsync(
            999, new ChangeRepairOrderStatusDto { Status = RepairOrderStatuses.Approved }));

        await _uow.RepairOrders.DidNotReceiveWithAnyArgs().ChangeStatusAsync(default, default!, default, default);
    }

    [Fact]
    public async Task CreateAsync_ValidOrder_SnapshotsNameAndPriceAndCommits()
    {
        var part = new CatalogItem { Id = 2, Name = "Battery", ItemType = CatalogItemTypes.Part, Price = 1450m };
        _uow.CatalogItems.GetByIdAsync(part.Id, Arg.Any<CancellationToken>()).Returns(part);
        _uow.CatalogItems.DecreaseStockAsync(part.Id, 2, Arg.Any<CancellationToken>()).Returns(true);

        var created = await _service.CreateAsync(NewOrder(new AddRepairOrderItemDto { CatalogItemId = 2, Quantity = 2 }));

        Assert.Equal(OrderId, created.Id);
        await _uow.RepairOrders.Received(1).AddItemAsync(
            Arg.Is<RepairOrderItem>(item =>
                item.RepairOrderId == OrderId && item.ItemName == "Battery" && item.UnitPrice == 1450m),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ServiceItem_DoesNotTouchStock()
    {
        var service = new CatalogItem { Id = 6, Name = "Diagnostics", ItemType = CatalogItemTypes.Service, Price = 300m };
        _uow.CatalogItems.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        await _service.CreateAsync(NewOrder(new AddRepairOrderItemDto { CatalogItemId = 6, Quantity = 1 }));

        await _uow.CatalogItems.DidNotReceiveWithAnyArgs().DecreaseStockAsync(default, default);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_InsufficientStock_ThrowsConflictAndRollsBack()
    {
        var part = new CatalogItem { Id = 1, Name = "Screen", ItemType = CatalogItemTypes.Part, Price = 4200m };
        _uow.CatalogItems.GetByIdAsync(part.Id, Arg.Any<CancellationToken>()).Returns(part);
        _uow.CatalogItems.DecreaseStockAsync(part.Id, 99, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.CreateAsync(
            NewOrder(new AddRepairOrderItemDto { CatalogItemId = 1, Quantity = 99 })));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_CustomerDoesNotExist_ThrowsNotFoundAndRollsBack()
    {
        var input = NewOrder();
        input.CustomerId = 777;
        _uow.Customers.GetByIdAsync(777, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(input));

        await _uow.RepairOrders.DidNotReceiveWithAnyArgs().AddAsync(default!);
        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateItems_ThrowsValidationBeforeTransaction()
    {
        var input = NewOrder(
            new AddRepairOrderItemDto { CatalogItemId = 5, Quantity = 1 },
            new AddRepairOrderItemDto { CatalogItemId = 5, Quantity = 2 });

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(input));

        await _uow.DidNotReceiveWithAnyArgs().BeginTransactionAsync();
    }

    [Fact]
    public async Task CreateAsync_DiscountAboveLineAmount_ThrowsValidationAndRollsBack()
    {
        var service = new CatalogItem { Id = 6, Name = "Diagnostics", ItemType = CatalogItemTypes.Service, Price = 300m };
        _uow.CatalogItems.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(
            NewOrder(new AddRepairOrderItemDto { CatalogItemId = 6, Quantity = 1, Discount = 301m })));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItemAsync_OrderIsReady_ThrowsConflict()
    {
        _uow.RepairOrders.GetWithItemsAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(new RepairOrder { Id = OrderId, Status = RepairOrderStatuses.Ready });

        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.AddItemAsync(
            OrderId, new AddRepairOrderItemDto { CatalogItemId = 5, Quantity = 1 }));

        await _uow.RepairOrders.DidNotReceiveWithAnyArgs().AddItemAsync(default!);
        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_OrderIsNotClosed_ThrowsConflict()
    {
        _uow.RepairOrders.GetByIdAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(new RepairOrder { Id = OrderId, Status = RepairOrderStatuses.InRepair });

        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.DeleteAsync(OrderId));

        await _uow.RepairOrders.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [Fact]
    public async Task UpdateDetailsAsync_InvalidRowVersion_ThrowsValidation()
    {
        var input = new UpdateRepairOrderDetailsDto { DefectDescription = "Does not boot", RowVersion = "not-base64" };

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateDetailsAsync(OrderId, input));
    }

    private static CreateRepairOrderDto NewOrder(params AddRepairOrderItemDto[] items) => new()
    {
        CustomerId = CustomerId,
        DeviceType = DeviceTypes.Smartphone,
        DeviceModel = "iPhone 13",
        DefectDescription = "Battery drains quickly",
        Items = [.. items]
    };
}

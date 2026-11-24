using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repairs.Api.Grpc;
using Repairs.Bll.Dtos.RepairOrders;
using Repairs.Bll.Interfaces;
using Repairs.Domain.Constants;
using Repairs.Domain.Exceptions;
using Repairs.Grpc;

namespace Repairs.Tests;

public class RepairsGrpcServiceTests
{
    private readonly IRepairOrderService _orders = Substitute.For<IRepairOrderService>();
    private readonly RepairsGrpcService _service;
    private readonly ServerCallContext _context = new TestServerCallContext("/repairs.RepairsService/GetRepairOrder");

    public RepairsGrpcServiceTests() => _service = new RepairsGrpcService(_orders);

    [Fact]
    public async Task GetRepairOrder_NonPositiveId_ReturnsInvalidArgument()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(
            () => _service.GetRepairOrder(new GetRepairOrderRequest { Id = -1 }, _context));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task GetRepairOrder_MissingOrder_ReturnsNotFound()
    {
        _orders.GetByIdAsync(404, Arg.Any<CancellationToken>()).ThrowsAsync(new NotFoundException("Repair order 404 not found"));

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => _service.GetRepairOrder(new GetRepairOrderRequest { Id = 404 }, _context));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetRepairOrder_ExistingOrder_MapsStatusMoneyAndSnapshot()
    {
        _orders.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new RepairOrderDto
        {
            Id = 1,
            CustomerId = 3,
            Status = RepairOrderStatuses.InRepair,
            AcceptedAt = new DateTime(2026, 10, 1, 9, 15, 0),
            Device = new DeviceDto { DeviceType = "Smartphone", DeviceModel = "iPhone 13", DefectDescription = "Screen" },
            Items = [new RepairOrderItemDto { CatalogItemId = 1, ItemName = "Screen", UnitPrice = 3900m, Quantity = 1, LineTotal = 3900m }],
            TotalAmount = 3900.5m
        });

        var reply = await _service.GetRepairOrder(new GetRepairOrderRequest { Id = 1 }, _context);

        Assert.Equal(RepairOrderStatus.InRepair, reply.Status);
        Assert.Equal(3900, reply.TotalAmount.Units);
        Assert.Equal(500_000_000, reply.TotalAmount.Nanos);
        Assert.Equal("Screen", Assert.Single(reply.Items).ItemName);
        Assert.Null(reply.IssuedAt);
        Assert.False(reply.Device.HasDeviceBrand);
    }
}

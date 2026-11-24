using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Repairs.Bll.Dtos.RepairOrders;
using Repairs.Bll.Interfaces;
using Repairs.Domain.Constants;
using Repairs.Domain.Exceptions;
using Repairs.Grpc;
using Device = Repairs.Grpc.Device;
using RepairOrderItem = Repairs.Grpc.RepairOrderItem;

namespace Repairs.Api.Grpc;

public sealed class RepairsGrpcService(IRepairOrderService repairOrders) : RepairsService.RepairsServiceBase
{
    private static readonly IReadOnlyDictionary<string, RepairOrderStatus> Statuses =
        new Dictionary<string, RepairOrderStatus>
        {
            [RepairOrderStatuses.Accepted] = RepairOrderStatus.Accepted,
            [RepairOrderStatuses.Diagnosed] = RepairOrderStatus.Diagnosed,
            [RepairOrderStatuses.Approved] = RepairOrderStatus.Approved,
            [RepairOrderStatuses.InRepair] = RepairOrderStatus.InRepair,
            [RepairOrderStatuses.Ready] = RepairOrderStatus.Ready,
            [RepairOrderStatuses.Paid] = RepairOrderStatus.Paid,
            [RepairOrderStatuses.Issued] = RepairOrderStatus.Issued,
            [RepairOrderStatuses.Cancelled] = RepairOrderStatus.Cancelled
        };

    public override async Task<RepairOrderReply> GetRepairOrder(GetRepairOrderRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be positive"));

        try
        {
            return ToReply(await repairOrders.GetByIdAsync(request.Id, context.CancellationToken));
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (BusinessConflictException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    private static RepairOrderReply ToReply(RepairOrderDto order)
    {
        var reply = new RepairOrderReply
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = Statuses.GetValueOrDefault(order.Status, RepairOrderStatus.Unspecified),
            AcceptedAt = ToTimestamp(order.AcceptedAt),
            TotalAmount = DecimalValue.FromDecimal(order.TotalAmount),
            Items =
            {
                order.Items.Select(item => new RepairOrderItem
                {
                    CatalogItemId = item.CatalogItemId,
                    ItemName = item.ItemName,
                    UnitPrice = DecimalValue.FromDecimal(item.UnitPrice),
                    Quantity = item.Quantity,
                    Discount = DecimalValue.FromDecimal(item.Discount),
                    LineTotal = DecimalValue.FromDecimal(item.LineTotal)
                })
            }
        };

        if (order.IssuedAt is { } issuedAt)
            reply.IssuedAt = ToTimestamp(issuedAt);
        if (order.Device is { } device)
            reply.Device = ToDevice(device);

        return reply;
    }

    private static Device ToDevice(DeviceDto device)
    {
        var reply = new Device
        {
            DeviceType = device.DeviceType,
            DeviceModel = device.DeviceModel,
            DefectDescription = device.DefectDescription
        };
        if (device.DeviceBrand is { } brand)
            reply.DeviceBrand = brand;
        if (device.DiagnosticNotes is { } notes)
            reply.DiagnosticNotes = notes;
        return reply;
    }

    private static Timestamp ToTimestamp(DateTime utc)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}

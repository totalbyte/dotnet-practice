using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Repairs.Bll.Dtos;
using Repairs.Bll.Dtos.RepairOrders;
using Repairs.Bll.Interfaces;
using Repairs.Dal.Interfaces;
using Repairs.Domain.Constants;
using Repairs.Domain.Exceptions;
using Repairs.Domain.Models;

namespace Repairs.Bll.Services;

public sealed class RepairOrderService(IUnitOfWork uow, IMapper mapper, ILogger<RepairOrderService> logger)
    : IRepairOrderService
{
    private const string AcceptedComment = "Device accepted for repair";
    private const int RowVersionLength = 8;

    public async Task<PagedResultDto<RepairOrderSummaryDto>> SearchAsync(
        RepairOrderQueryDto query, CancellationToken ct = default)
    {
        var result = await uow.RepairOrders.SearchAsync(
            query.Status, query.CustomerId, query.Page, query.PageSize, ct);

        return new PagedResultDto<RepairOrderSummaryDto>
        {
            Items = mapper.Map<IReadOnlyList<RepairOrderSummaryDto>>(result.Items),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount
        };
    }

    public async Task<RepairOrderDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var order = await uow.RepairOrders.GetWithItemsAsync(id, ct)
                    ?? throw new NotFoundException($"Repair order {id} not found");

        order.History = [.. await uow.RepairOrders.GetStatusHistoryAsync(id, ct)];

        return mapper.Map<RepairOrderDto>(order);
    }

    public async Task<RepairOrderDto> CreateAsync(CreateRepairOrderDto input, CancellationToken ct = default)
    {
        EnsureNoDuplicateItems(input.Items);

        long orderId;

        await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            _ = await uow.Customers.GetByIdAsync(input.CustomerId, ct)
                ?? throw new NotFoundException($"Customer {input.CustomerId} not found");

            var order = new RepairOrder { CustomerId = input.CustomerId, CreatedBy = input.CreatedBy };
            orderId = await uow.RepairOrders.AddAsync(order, ct);

            var details = mapper.Map<RepairOrderDetails>(input);
            details.RepairOrderId = orderId;
            await uow.RepairOrders.AddDetailsAsync(details, ct);

            await uow.RepairOrders.AddStatusHistoryAsync(new RepairOrderStatusHistory
            {
                RepairOrderId = orderId,
                ToStatus = RepairOrderStatuses.Accepted,
                Comment = AcceptedComment,
                ChangedBy = input.CreatedBy
            }, ct);

            foreach (var item in input.Items)
                await AddItemWithStockReservationAsync(orderId, item, input.CreatedBy, ct);

            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);
            throw;
        }

        logger.LogInformation(
            "Repair order {RepairOrderId} created with {ItemsCount} items", orderId, input.Items.Count);

        return await GetByIdAsync(orderId, ct);
    }

    public async Task<RepairOrderDto> AddItemAsync(
        long id, AddRepairOrderItemDto input, CancellationToken ct = default)
    {
        await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var order = await uow.RepairOrders.GetWithItemsAsync(id, ct)
                        ?? throw new NotFoundException($"Repair order {id} not found");

            if (!RepairOrderStatuses.ItemsEditable.Contains(order.Status))
                throw new BusinessConflictException(
                    $"Items cannot be changed while repair order {id} is in status {order.Status}");

            if (order.Items.Any(item => item.CatalogItemId == input.CatalogItemId))
                throw new BusinessConflictException(
                    $"Catalog item {input.CatalogItemId} is already in repair order {id}");

            await AddItemWithStockReservationAsync(id, input, input.AddedBy, ct);

            await uow.CommitAsync(ct);
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);
            throw;
        }

        logger.LogInformation(
            "Catalog item {CatalogItemId} added to repair order {RepairOrderId}", input.CatalogItemId, id);

        return await GetByIdAsync(id, ct);
    }

    public async Task ChangeStatusAsync(long id, ChangeRepairOrderStatusDto input, CancellationToken ct = default)
    {
        var order = await uow.RepairOrders.GetByIdAsync(id, ct)
                    ?? throw new NotFoundException($"Repair order {id} not found");

        await uow.RepairOrders.ChangeStatusAsync(id, input.Status, input.ChangedBy, input.Comment, ct);

        logger.LogInformation(
            "Repair order {RepairOrderId} moved from {FromStatus} to {ToStatus}", id, order.Status, input.Status);
    }

    public async Task UpdateDetailsAsync(long id, UpdateRepairOrderDetailsDto input, CancellationToken ct = default)
    {
        var rowVersion = new byte[RowVersionLength];
        if (!Convert.TryFromBase64String(input.RowVersion, rowVersion, out var bytesWritten)
            || bytesWritten != RowVersionLength)
            throw new ValidationException("RowVersion must be the Base64 value returned with the repair order");

        _ = await uow.RepairOrders.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Repair order {id} not found");

        var details = mapper.Map<RepairOrderDetails>(input);
        details.RepairOrderId = id;
        details.RowVersion = rowVersion;

        await uow.RepairOrders.UpdateDetailsAsync(details, ct);

        logger.LogInformation("Details of repair order {RepairOrderId} updated", id);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var order = await uow.RepairOrders.GetByIdAsync(id, ct)
                    ?? throw new NotFoundException($"Repair order {id} not found");

        if (!RepairOrderStatuses.Closed.Contains(order.Status))
            throw new BusinessConflictException("Only issued or cancelled repair orders can be deleted");

        await uow.RepairOrders.DeleteAsync(id, ct);

        logger.LogInformation("Repair order {RepairOrderId} deleted", id);
    }

    private async Task AddItemWithStockReservationAsync(
        long orderId, AddRepairOrderItemDto input, string? addedBy, CancellationToken ct)
    {
        var catalogItem = await uow.CatalogItems.GetByIdAsync(input.CatalogItemId, ct)
                          ?? throw new NotFoundException($"Catalog item {input.CatalogItemId} not found");

        if (input.Discount > catalogItem.Price * input.Quantity)
            throw new ValidationException(
                $"Discount for catalog item {catalogItem.Id} exceeds the line amount");

        await uow.RepairOrders.AddItemAsync(TakeSnapshot(orderId, catalogItem, input, addedBy), ct);

        if (catalogItem.ItemType == CatalogItemTypes.Part
            && !await uow.CatalogItems.DecreaseStockAsync(catalogItem.Id, input.Quantity, ct))
            throw new BusinessConflictException($"Insufficient stock for catalog item {catalogItem.Id}");
    }

    private static RepairOrderItem TakeSnapshot(
        long orderId, CatalogItem catalogItem, AddRepairOrderItemDto input, string? addedBy) => new()
    {
        RepairOrderId = orderId,
        CatalogItemId = catalogItem.Id,
        ItemName = catalogItem.Name,
        UnitPrice = catalogItem.Price,
        Quantity = input.Quantity,
        Discount = input.Discount,
        AddedBy = addedBy
    };

    private static void EnsureNoDuplicateItems(IReadOnlyCollection<AddRepairOrderItemDto> items)
    {
        var duplicateId = items
            .GroupBy(item => item.CatalogItemId)
            .Where(group => group.Count() > 1)
            .Select(group => (long?)group.Key)
            .FirstOrDefault();

        if (duplicateId is not null)
            throw new ValidationException($"Catalog item {duplicateId} is listed more than once");
    }
}

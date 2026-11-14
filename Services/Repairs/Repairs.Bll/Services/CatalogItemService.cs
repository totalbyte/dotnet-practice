using AutoMapper;
using Microsoft.Extensions.Logging;
using Repairs.Bll.Dtos.CatalogItems;
using Repairs.Bll.Interfaces;
using Repairs.Dal.Interfaces;
using Repairs.Domain.Exceptions;

namespace Repairs.Bll.Services;

public sealed class CatalogItemService(IUnitOfWork uow, IMapper mapper, ILogger<CatalogItemService> logger)
    : ICatalogItemService
{
    public async Task<IReadOnlyList<CatalogItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await uow.CatalogItems.GetAllAsync(ct);
        return mapper.Map<IReadOnlyList<CatalogItemDto>>(items);
    }

    public async Task<CatalogItemDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var item = await uow.CatalogItems.GetByIdAsync(id, ct)
                   ?? throw new NotFoundException($"Catalog item {id} not found");

        return mapper.Map<CatalogItemDto>(item);
    }

    public async Task UpdateStockAsync(long id, UpdateStockDto input, CancellationToken ct = default)
    {
        if (!await uow.CatalogItems.SetStockAsync(id, input.Stock, ct))
            throw new NotFoundException($"Catalog item {id} not found");

        logger.LogInformation("Stock of catalog item {CatalogItemId} set to {Stock}", id, input.Stock);
    }
}

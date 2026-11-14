using AutoMapper;
using Microsoft.Extensions.Logging;
using Repairs.Bll.Dtos.Customers;
using Repairs.Bll.Interfaces;
using Repairs.Dal.Interfaces;
using Repairs.Domain.Exceptions;
using Repairs.Domain.Models;

namespace Repairs.Bll.Services;

public sealed class CustomerService(IUnitOfWork uow, IMapper mapper, ILogger<CustomerService> logger)
    : ICustomerService
{
    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(string? phone, CancellationToken ct = default)
    {
        var customers = string.IsNullOrWhiteSpace(phone)
            ? await uow.Customers.GetAllAsync(ct)
            : await uow.Customers.FindByPhoneAsync(phone.Trim(), ct);

        return mapper.Map<IReadOnlyList<CustomerDto>>(customers);
    }

    public async Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var customer = await uow.Customers.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException($"Customer {id} not found");

        return mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto input, CancellationToken ct = default)
    {
        var id = await uow.Customers.AddAsync(mapper.Map<Customer>(input), ct);
        logger.LogInformation("Customer {CustomerId} created", id);

        return await GetByIdAsync(id, ct);
    }

    public async Task UpdateAsync(long id, UpdateCustomerDto input, CancellationToken ct = default)
    {
        var customer = mapper.Map<Customer>(input);
        customer.Id = id;

        if (!await uow.Customers.UpdateAsync(customer, ct))
            throw new NotFoundException($"Customer {id} not found");

        logger.LogInformation("Customer {CustomerId} updated", id);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        _ = await uow.Customers.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Customer {id} not found");

        if (await uow.RepairOrders.HasOpenOrdersAsync(id, ct))
            throw new BusinessConflictException($"Customer {id} has unfinished repair orders");

        await uow.Customers.DeleteAsync(id, ct);
        logger.LogInformation("Customer {CustomerId} deleted", id);
    }
}

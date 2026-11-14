using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Repairs.Bll.Services;
using Repairs.Dal.Interfaces;
using Repairs.Domain.Exceptions;
using Repairs.Domain.Models;

namespace Repairs.Tests;

public class CustomerServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _service = new CustomerService(_uow, TestMapper.Create(), NullLogger<CustomerService>.Instance);
    }

    [Fact]
    public async Task GetByIdAsync_CustomerDoesNotExist_ThrowsNotFound()
    {
        _uow.Customers.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(5));
    }

    [Fact]
    public async Task DeleteAsync_CustomerHasOpenOrders_ThrowsConflict()
    {
        _uow.Customers.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Customer { Id = 1 });
        _uow.RepairOrders.HasOpenOrdersAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<BusinessConflictException>(() => _service.DeleteAsync(1));

        await _uow.Customers.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [Fact]
    public async Task DeleteAsync_CustomerWithoutOpenOrders_SoftDeletes()
    {
        _uow.Customers.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Customer { Id = 1 });
        _uow.RepairOrders.HasOpenOrdersAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        await _service.DeleteAsync(1);

        await _uow.Customers.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }
}

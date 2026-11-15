using Ardalis.Specification;
using Catalog.Bll.Dtos;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Services;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Catalog.Tests;

public class CatalogItemServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CatalogItemService _service;

    public CatalogItemServiceTests()
    {
        _service = new CatalogItemService(
            _uow, TestSupport.CreateMapper(), NullLogger<CatalogItemService>.Instance);
    }

    private static CreateCatalogItemDto NewPart(int? brandId = null) => new()
    {
        Sku = "PRT-NEW",
        Name = "New part",
        ItemType = CatalogItemType.Part,
        Price = 120m,
        BrandId = brandId
    };

    private static CatalogItem ExistingPart(long id = 1)
        => new CatalogItem("PRT-OLD", "Old part", CatalogItemType.Part, 100m).WithId(id);

    [Fact]
    public async Task CreateAsync_SkuAlreadyExists_ThrowsConflictWithoutSaving()
    {
        _uow.CatalogItems.ExistsBySkuAsync("PRT-NEW", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(NewPart()));

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_BrandDoesNotExist_ThrowsValidationForBrandId()
    {
        _uow.Brands.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Brand?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(NewPart(99)));

        Assert.Contains(nameof(CreateCatalogItemDto.BrandId), exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_AddsItemAndSavesOnce()
    {
        CatalogItem? added = null;
        await _uow.CatalogItems.AddAsync(Arg.Do<CatalogItem>(item => added = item), Arg.Any<CancellationToken>());
        _uow.CatalogItems.GetWithDetailsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => added);

        var created = await _service.CreateAsync(NewPart());

        Assert.Equal("PRT-NEW", created.Sku);
        Assert.Equal("Part", created.ItemType);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_ItemDoesNotExist_ThrowsNotFound()
    {
        _uow.CatalogItems.GetWithDetailsAsync(5, Arg.Any<CancellationToken>()).Returns((CatalogItem?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.GetByIdAsync(5));
    }

    [Fact]
    public async Task GetListAsync_PageSizeAboveLimit_IsClampedAndTotalComesFromCount()
    {
        ISpecification<CatalogItem>? used = null;
        _uow.CatalogItems.CountAsync(Arg.Any<ISpecification<CatalogItem>>(), Arg.Any<CancellationToken>())
            .Returns(250);
        _uow.CatalogItems
            .ListAsync(Arg.Do<ISpecification<CatalogItem>>(spec => used = spec), Arg.Any<CancellationToken>())
            .Returns([ExistingPart()]);

        var page = await _service.GetListAsync(new CatalogItemQuery { Page = 2, PageSize = 500 });

        Assert.Equal(PagedQuery.MaxPageSize, page.PageSize);
        Assert.Equal(250, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasNext);
        Assert.True(page.HasPrevious);
        Assert.Equal(PagedQuery.MaxPageSize, used!.Take);
        Assert.Equal(PagedQuery.MaxPageSize, used.Skip);
    }

    [Fact]
    public async Task ChangePriceAsync_ExistingItem_ChangesPriceAndSavesOnce()
    {
        var item = ExistingPart();
        _uow.CatalogItems.GetWithDetailsAsync(1, Arg.Any<CancellationToken>()).Returns(item);

        var updated = await _service.ChangePriceAsync(1, new ChangePriceDto { Price = 175.5m });

        Assert.Equal(175.5m, updated.Price);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ExistingItem_MarksDeletedInsteadOfRemoving()
    {
        var item = ExistingPart();
        _uow.CatalogItems.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(item);

        await _service.DeleteAsync(1);

        Assert.True(item.IsDeleted);
        _uow.CatalogItems.DidNotReceive().Delete(Arg.Any<CatalogItem>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignCategoryAsync_CategoryDoesNotExist_ThrowsNotFound()
    {
        _uow.CatalogItems.GetWithDetailsAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingPart());
        _uow.Categories.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((Category?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _service.AssignCategoryAsync(1, 42, new AssignCategoryDto()));
    }

    [Fact]
    public async Task RemoveCategoryAsync_ItemIsNotInCategory_ThrowsNotFound()
    {
        _uow.CatalogItems.GetWithDetailsAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingPart());

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.RemoveCategoryAsync(1, 3));
    }
}

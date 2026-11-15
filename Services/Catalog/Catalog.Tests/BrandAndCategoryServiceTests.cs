using Ardalis.Specification;
using Catalog.Bll.Dtos.Brands;
using Catalog.Bll.Dtos.Categories;
using Catalog.Bll.Services;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Catalog.Tests;

public class BrandAndCategoryServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly BrandService _brands;
    private readonly CategoryService _categories;

    public BrandAndCategoryServiceTests()
    {
        var mapper = TestSupport.CreateMapper();
        _brands = new BrandService(_uow, mapper, NullLogger<BrandService>.Instance);
        _categories = new CategoryService(_uow, mapper, NullLogger<CategoryService>.Instance);
    }

    [Fact]
    public async Task CreateBrand_NameAlreadyExists_ThrowsConflict()
    {
        _uow.Brands.AnyAsync(Arg.Any<ISpecification<Brand>>(), Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _brands.CreateAsync(new CreateBrandDto { Name = "Apple" }));

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBrand_UsedByItems_ThrowsConflictAndKeepsBrand()
    {
        _uow.Brands.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Brand("Apple").WithId(1));
        _uow.CatalogItems.AnyAsync(Arg.Any<ISpecification<CatalogItem>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _brands.DeleteAsync(1));

        _uow.Brands.DidNotReceive().Delete(Arg.Any<Brand>());
    }

    [Fact]
    public async Task DeleteBrand_NotUsed_DeletesAndSavesOnce()
    {
        var brand = new Brand("Lenovo").WithId(5);
        _uow.Brands.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(brand);

        await _brands.DeleteAsync(5);

        _uow.Brands.Received(1).Delete(brand);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCategory_ParentDoesNotExist_ThrowsValidationForParent()
    {
        _uow.Categories.GetByIdAsync(77, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _categories.CreateAsync(
            new CreateCategoryDto { Name = "Cables", Slug = "cables", ParentCategoryId = 77 }));

        Assert.Contains(nameof(CreateCategoryDto.ParentCategoryId), exception.Errors.Keys);
    }

    [Fact]
    public async Task UpdateCategory_MovedUnderOwnSubcategory_ThrowsConflict()
    {
        var parts = new Category("Parts", "parts").WithId(1);
        var displays = new Category("Displays", "displays", parentCategoryId: 1).WithId(3);
        _uow.Categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(parts);
        _uow.Categories.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(displays);

        await Assert.ThrowsAsync<ConflictException>(() => _categories.UpdateAsync(
            1, new UpdateCategoryDto { Name = "Parts", Slug = "parts", ParentCategoryId = 3 }));

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCategory_HasSubcategories_ThrowsConflict()
    {
        _uow.Categories.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Category("Parts", "parts").WithId(1));
        _uow.Categories.AnyAsync(Arg.Any<ISpecification<Category>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _categories.DeleteAsync(1));
    }
}

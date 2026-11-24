using Catalog.Bll.Caching;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Bll.Interfaces;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Catalog.Tests;

public class CachedCatalogItemServiceTests
{
    private readonly ICatalogItemService _inner = Substitute.For<ICatalogItemService>();
    private readonly MemoryCache _memory = new(new MemoryCacheOptions { SizeLimit = 1024 });
    private readonly IDistributedCache _redis =
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    private readonly CachedCatalogItemService _service;

    public CachedCatalogItemServiceTests()
    {
        var options = Options.Create(new CatalogCacheOptions());
        var cache = new TwoLevelCache(_memory, _redis, new CatalogCacheMetrics(new TestMeterFactory(), _memory),
            options, TimeProvider.System, NullLogger<TwoLevelCache>.Instance);
        var invalidator = new CatalogCacheInvalidator(cache, Substitute.For<IUnitOfWork>(), options);
        _service = new CachedCatalogItemService(_inner, cache, invalidator, options);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new CatalogItemDetailsDto { Id = 1, Price = 100m });
    }

    [Fact]
    public async Task GetByIdAsync_RepeatedRead_HitsDatabaseOnce()
    {
        await _service.GetByIdAsync(1);
        await _service.GetByIdAsync(1);

        await _inner.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePriceAsync_InvalidatesCardSoNextReadIsFresh()
    {
        await _service.GetByIdAsync(1);
        _inner.ChangePriceAsync(1, Arg.Any<ChangePriceDto>(), Arg.Any<CancellationToken>())
            .Returns(new CatalogItemDetailsDto { Id = 1, Price = 150m });
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new CatalogItemDetailsDto { Id = 1, Price = 150m });

        await _service.ChangePriceAsync(1, new ChangePriceDto { Price = 150m });
        var card = await _service.GetByIdAsync(1);

        Assert.Equal(150m, card.Price);
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WriteFails_KeepsCachedCard()
    {
        await _service.GetByIdAsync(1);
        _inner.UpdateAsync(1, Arg.Any<UpdateCatalogItemDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntityNotFoundException("CatalogItem", 1));

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.UpdateAsync(1, new UpdateCatalogItemDto()));
        await _service.GetByIdAsync(1);

        await _inner.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ChangesListVersionSoListsAreReloaded()
    {
        var query = new CatalogItemQuery();
        _inner.GetListAsync(query, Arg.Any<CancellationToken>())
            .Returns(new Catalog.Bll.Dtos.PagedResult<CatalogItemDto>([], 0, 1, 20));

        await _service.GetListAsync(query);
        await _service.CreateAsync(new CreateCatalogItemDto());
        await _service.GetListAsync(query);

        await _inner.Received(2).GetListAsync(query, Arg.Any<CancellationToken>());
    }
}

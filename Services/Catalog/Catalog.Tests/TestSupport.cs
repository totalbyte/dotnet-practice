using AutoMapper;
using Catalog.Bll.Mapping;
using Microsoft.Extensions.Logging.Abstractions;

namespace Catalog.Tests;

internal static class TestSupport
{
    public static MapperConfiguration CreateMapperConfiguration() => new(
        cfg =>
        {
            cfg.AddProfile<CatalogItemProfile>();
            cfg.AddProfile<CategoryProfile>();
            cfg.AddProfile<BrandProfile>();
        },
        NullLoggerFactory.Instance);

    public static IMapper CreateMapper() => CreateMapperConfiguration().CreateMapper();

    public static T WithId<T>(this T entity, object id) where T : class
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}

using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Repairs.Bll.Mapping;

namespace Repairs.Tests;

internal static class TestMapper
{
    public static MapperConfiguration CreateConfiguration() => new(
        cfg =>
        {
            cfg.AddProfile<CustomerProfile>();
            cfg.AddProfile<CatalogItemProfile>();
            cfg.AddProfile<RepairOrderProfile>();
        },
        NullLoggerFactory.Instance);

    public static IMapper Create() => CreateConfiguration().CreateMapper();
}

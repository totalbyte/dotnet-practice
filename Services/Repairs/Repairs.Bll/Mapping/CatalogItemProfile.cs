using AutoMapper;
using Repairs.Bll.Dtos.CatalogItems;
using Repairs.Domain.Models;

namespace Repairs.Bll.Mapping;

public sealed class CatalogItemProfile : Profile
{
    public CatalogItemProfile()
    {
        CreateMap<CatalogItem, CatalogItemDto>();
    }
}

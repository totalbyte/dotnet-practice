using AutoMapper;
using Catalog.Bll.Dtos.CatalogItems;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Mapping;

public sealed class CatalogItemProfile : Profile
{
    public CatalogItemProfile()
    {
        CreateMap<CatalogItem, CatalogItemDto>();
        CreateMap<CatalogItem, CatalogItemSummaryDto>();

        CreateMap<CatalogItem, CatalogItemDetailsDto>()
            .ForMember(d => d.Description, o => o.MapFrom(i => i.Detail!.Description))
            .ForMember(d => d.WarrantyMonths, o => o.MapFrom(i => i.Detail!.WarrantyMonths))
            .ForMember(d => d.EstimatedDurationMinutes, o => o.MapFrom(i => i.Detail!.EstimatedDurationMinutes))
            .ForMember(d => d.Specifications, o => o.MapFrom(i => i.Detail!.Specifications));

        CreateMap<CatalogItemCategory, ItemCategoryDto>()
            .ForCtorParam(nameof(ItemCategoryDto.Name), o => o.MapFrom(ic => ic.Category.Name))
            .ForCtorParam(nameof(ItemCategoryDto.Slug), o => o.MapFrom(ic => ic.Category.Slug));
    }
}

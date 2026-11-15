using AutoMapper;
using Catalog.Bll.Dtos.Categories;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Mapping;

public sealed class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();

        CreateMap<Category, CategoryWithItemsDto>()
            .ForCtorParam(nameof(CategoryWithItemsDto.Items), o => o.MapFrom(c => c.ItemCategories));

        CreateMap<CatalogItemCategory, CategoryItemDto>()
            .ForCtorParam(nameof(CategoryItemDto.Sku), o => o.MapFrom(ic => ic.CatalogItem.Sku))
            .ForCtorParam(nameof(CategoryItemDto.Name), o => o.MapFrom(ic => ic.CatalogItem.Name))
            .ForCtorParam(nameof(CategoryItemDto.Price), o => o.MapFrom(ic => ic.CatalogItem.Price));
    }
}

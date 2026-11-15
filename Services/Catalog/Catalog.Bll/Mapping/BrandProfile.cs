using AutoMapper;
using Catalog.Bll.Dtos.Brands;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Mapping;

public sealed class BrandProfile : Profile
{
    public BrandProfile() => CreateMap<Brand, BrandDto>();
}

namespace Catalog.Bll.Dtos.Brands;

public sealed record BrandDto(int Id, string Name, string? Country, DateTime CreatedAt);

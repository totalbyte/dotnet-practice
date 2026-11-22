namespace Platform.Aggregator.Dtos;

public sealed record RepairOrderCardDto(
    RepairOrderInfoDto Order,
    IReadOnlyList<RepairOrderPartDto> Parts,
    IReadOnlyList<ReviewInfoDto> Reviews,
    IReadOnlyList<string> Warnings);

public sealed record RepairOrderInfoDto(
    long Id,
    long CustomerId,
    string Status,
    DateTime AcceptedAt,
    DateTime? IssuedAt,
    DeviceInfoDto? Device,
    decimal TotalAmount);

public sealed record DeviceInfoDto(
    string Type,
    string? Brand,
    string Model,
    string DefectDescription,
    string? DiagnosticNotes);

public sealed record RepairOrderPartDto(
    long CatalogItemId,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotal,
    CatalogItemInfoDto? Catalog);

public sealed record CatalogItemInfoDto(
    string Sku,
    string Name,
    string ItemType,
    string? BrandName,
    decimal CurrentPrice,
    bool PriceChanged,
    bool IsActive,
    int WarrantyMonths);

public sealed record ReviewInfoDto(
    string Id,
    string ReviewType,
    int Rating,
    string? Title,
    string Text,
    string? AuthorName,
    long? CatalogItemId,
    DateTime CreatedAt);

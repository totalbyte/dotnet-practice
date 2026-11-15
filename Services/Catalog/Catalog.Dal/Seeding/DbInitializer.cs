using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Seeding;

public static class DbInitializer
{
    private const string SeedAuthor = "seed";

    private const int Apple = 1, Samsung = 2, Kingston = 3, Arctic = 4;

    private const int Displays = 3, Batteries = 4, Storage = 5, Consumables = 6, ForSmartphones = 7,
        ForLaptops = 8, Diagnostics = 9, SmartphoneRepair = 10, LaptopRepair = 11;

    public static async Task SeedAsync(CatalogDbContext db, CancellationToken ct = default)
    {
        if (await db.CatalogItems.IgnoreQueryFilters().AnyAsync(ct))
            return;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        long id = 0;
        foreach (var item in CreateItems())
        {
            db.CatalogItems.Add(item);
            db.Entry(item).Property(i => i.Id).CurrentValue = ++id;
        }

        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[CatalogItems] ON", ct);
        await db.SaveChangesAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[CatalogItems] OFF", ct);

        await transaction.CommitAsync(ct);
    }

    private static IEnumerable<CatalogItem> CreateItems()
    {
        yield return Part("PRT-DSP-IP13", "Екран OLED для iPhone 13", 4200.00m, Apple,
            "Дисплейний модуль OLED у зборі з сенсором", 6,
            """{"diagonal":"6.1\"","technology":"OLED","resolution":"2532x1170"}""",
            (Displays, At(9, 1, 9, 0), true), (ForSmartphones, At(9, 1, 9, 0), false));

        yield return Part("PRT-BAT-IP13", "Акумулятор для iPhone 13", 1450.00m, Apple,
            "Літій-іонний акумулятор, сумісний з iPhone 13", 6,
            """{"capacityMah":3227,"voltage":3.85}""",
            (Batteries, At(9, 1, 9, 5), true), (ForSmartphones, At(9, 1, 9, 5), false));

        yield return Part("PRT-BAT-S22", "Акумулятор для Samsung Galaxy S22", 1250.00m, Samsung,
            "Акумулятор EB-BS901ABY для Galaxy S22", 6,
            """{"capacityMah":3700,"voltage":3.88}""",
            (Batteries, At(9, 3, 14, 20), true), (ForSmartphones, At(9, 3, 14, 20), false));

        yield return Part("PRT-SSD-NV2-500", "SSD Kingston NV2 500 ГБ", 1890.00m, Kingston,
            "Твердотільний накопичувач M.2 NVMe", 36,
            """{"capacityGb":500,"interface":"NVMe PCIe 4.0","formFactor":"M.2 2280"}""",
            (Storage, At(9, 10, 11, 0), true), (ForLaptops, At(9, 15, 16, 30), false));

        yield return Part("PRT-TP-MX4-4", "Термопаста Arctic MX-4, 4 г", 320.00m, Arctic,
            "Термопаста для процесорів і відеокарт", 0,
            """{"weightG":4,"conductivity":"8.5 W/mK"}""",
            (Consumables, At(9, 10, 11, 10), true), (ForLaptops, At(10, 2, 10, 0), false));

        yield return Service("SRV-DIAG", "Діагностика пристрою", 300.00m,
            "Повна діагностика з письмовим висновком", 0, 60, Diagnostics, At(9, 1, 8, 0));

        yield return Service("SRV-DSP-PHONE", "Заміна екрана смартфона", 800.00m,
            "Заміна дисплейного модуля з перевіркою сенсора", 3, 90, SmartphoneRepair, At(9, 1, 8, 10));

        yield return Service("SRV-CLEAN-NB", "Чистка ноутбука від пилу", 650.00m,
            "Розбирання, чистка системи охолодження, збирання", 1, 120, LaptopRepair, At(9, 1, 8, 20));

        yield return Service("SRV-BAT-PHONE", "Заміна акумулятора смартфона", 450.00m,
            "Заміна акумулятора з калібруванням", 3, 45, SmartphoneRepair, At(9, 1, 8, 15));

        yield return Service("SRV-SSD-MIGR", "Встановлення SSD з перенесенням даних", 700.00m,
            "Монтаж SSD та клонування системи зі старого диска", 1, 150, LaptopRepair, At(9, 12, 12, 0));
    }

    private static CatalogItem Part(
        string sku, string name, decimal price, int brandId, string description, int warrantyMonths,
        string specifications, params (int CategoryId, DateTime AddedAt, bool IsPrimary)[] categories)
    {
        var item = new CatalogItem(sku, name, CatalogItemType.Part, price, brandId, SeedAuthor);
        item.Describe(description, warrantyMonths, null, specifications);
        foreach (var (categoryId, addedAt, isPrimary) in categories)
            item.AssignCategory(categoryId, isPrimary, addedAt);
        return item;
    }

    private static CatalogItem Service(
        string sku, string name, decimal price, string description, int warrantyMonths,
        int durationMinutes, int categoryId, DateTime addedAt)
    {
        var item = new CatalogItem(sku, name, CatalogItemType.Service, price, createdBy: SeedAuthor);
        item.Describe(description, warrantyMonths, durationMinutes, null);
        item.AssignCategory(categoryId, isPrimary: true, addedAt);
        return item;
    }

    private static DateTime At(int month, int day, int hour, int minute)
        => new(2026, month, day, hour, minute, 0, DateTimeKind.Utc);
}

/* Проєкт №2 - CatalogDb. Довідникові та початкові дані; запускати після schema.sql.
   Ідемпотентно: кожна вставка перевіряє наявність запису (WHERE NOT EXISTS).
   Id позицій 1..10 зафіксовано: їх використовують RepairsDb і MongoDB. */

USE CatalogDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

-- Довідник: бренди
INSERT INTO dbo.Brands (Name, Country)
SELECT v.Name, v.Country
FROM (VALUES
    (N'Apple',    N'США'),
    (N'Samsung',  N'Південна Корея'),
    (N'Kingston', N'США'),
    (N'Arctic',   N'Швейцарія'),
    (N'Lenovo',   N'Китай')
) AS v (Name, Country)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Brands b WHERE b.Name = v.Name);
GO

-- Довідник: категорії
INSERT INTO dbo.Categories (Name, Slug, ParentCategoryId, DisplayOrder)
SELECT v.Name, v.Slug, NULL, v.DisplayOrder
FROM (VALUES
    (N'Запчастини', N'parts',    1),
    (N'Послуги',    N'services', 2)
) AS v (Name, Slug, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Slug = v.Slug);

INSERT INTO dbo.Categories (Name, Slug, ParentCategoryId, DisplayOrder)
SELECT v.Name, v.Slug, p.Id, v.DisplayOrder
FROM (VALUES
    (N'Екрани та дисплеї',     N'displays',            N'parts',    1),
    (N'Акумулятори',           N'batteries',           N'parts',    2),
    (N'Накопичувачі',          N'storage',             N'parts',    3),
    (N'Витратні матеріали',    N'consumables',         N'parts',    4),
    (N'Для смартфонів',        N'for-smartphones',     N'parts',    5),
    (N'Для ноутбуків',         N'for-laptops',         N'parts',    6),
    (N'Діагностика',           N'diagnostics',         N'services', 1),
    (N'Ремонт смартфонів',     N'smartphone-repair',   N'services', 2),
    (N'Ремонт ноутбуків',      N'laptop-repair',       N'services', 3)
) AS v (Name, Slug, ParentSlug, DisplayOrder)
JOIN dbo.Categories p ON p.Slug = v.ParentSlug
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Slug = v.Slug);
GO

-- Позиції каталогу
SET IDENTITY_INSERT dbo.CatalogItems ON;

INSERT INTO dbo.CatalogItems (Id, Sku, Name, ItemType, Price, BrandId, CreatedBy)
SELECT v.Id, v.Sku, v.Name, v.ItemType, v.Price, b.Id, N'seed'
FROM (VALUES
    (1,  N'PRT-DSP-IP13',  N'Екран OLED для iPhone 13',              N'Part',    4200.00, N'Apple'),
    (2,  N'PRT-BAT-IP13',  N'Акумулятор для iPhone 13',              N'Part',    1450.00, N'Apple'),
    (3,  N'PRT-BAT-S22',   N'Акумулятор для Samsung Galaxy S22',     N'Part',    1250.00, N'Samsung'),
    (4,  N'PRT-SSD-NV2-500', N'SSD Kingston NV2 500 ГБ',             N'Part',    1890.00, N'Kingston'),
    (5,  N'PRT-TP-MX4-4',  N'Термопаста Arctic MX-4, 4 г',           N'Part',     320.00, N'Arctic'),
    (6,  N'SRV-DIAG',      N'Діагностика пристрою',                  N'Service',  300.00, NULL),
    (7,  N'SRV-DSP-PHONE', N'Заміна екрана смартфона',               N'Service',  800.00, NULL),
    (8,  N'SRV-CLEAN-NB',  N'Чистка ноутбука від пилу',              N'Service',  650.00, NULL),
    (9,  N'SRV-BAT-PHONE', N'Заміна акумулятора смартфона',          N'Service',  450.00, NULL),
    (10, N'SRV-SSD-MIGR',  N'Встановлення SSD з перенесенням даних', N'Service',  700.00, NULL)
) AS v (Id, Sku, Name, ItemType, Price, BrandName)
LEFT JOIN dbo.Brands b ON b.Name = v.BrandName
WHERE NOT EXISTS (SELECT 1 FROM dbo.CatalogItems ci WHERE ci.Id = v.Id OR ci.Sku = v.Sku);

SET IDENTITY_INSERT dbo.CatalogItems OFF;
GO

-- Розширені описи (1:1)
INSERT INTO dbo.CatalogItemDetails (CatalogItemId, Description, WarrantyMonths, EstimatedDurationMinutes, Specifications)
SELECT ci.Id, v.Description, v.WarrantyMonths, v.EstimatedDurationMinutes, v.Specifications
FROM (VALUES
    (N'PRT-DSP-IP13',    N'Дисплейний модуль OLED у зборі з сенсором',        6,  NULL, N'{"diagonal":"6.1\"","technology":"OLED","resolution":"2532x1170"}'),
    (N'PRT-BAT-IP13',    N'Літій-іонний акумулятор, сумісний з iPhone 13',    6,  NULL, N'{"capacityMah":3227,"voltage":3.85}'),
    (N'PRT-BAT-S22',     N'Акумулятор EB-BS901ABY для Galaxy S22',            6,  NULL, N'{"capacityMah":3700,"voltage":3.88}'),
    (N'PRT-SSD-NV2-500', N'Твердотільний накопичувач M.2 NVMe',               36, NULL, N'{"capacityGb":500,"interface":"NVMe PCIe 4.0","formFactor":"M.2 2280"}'),
    (N'PRT-TP-MX4-4',    N'Термопаста для процесорів і відеокарт',            0,  NULL, N'{"weightG":4,"conductivity":"8.5 W/mK"}'),
    (N'SRV-DIAG',        N'Повна діагностика з письмовим висновком',          0,  60,   NULL),
    (N'SRV-DSP-PHONE',   N'Заміна дисплейного модуля з перевіркою сенсора',   3,  90,   NULL),
    (N'SRV-CLEAN-NB',    N'Розбирання, чистка системи охолодження, збирання', 1,  120,  NULL),
    (N'SRV-BAT-PHONE',   N'Заміна акумулятора з калібруванням',               3,  45,   NULL),
    (N'SRV-SSD-MIGR',    N'Монтаж SSD та клонування системи зі старого диска', 1, 150,  NULL)
) AS v (Sku, Description, WarrantyMonths, EstimatedDurationMinutes, Specifications)
JOIN dbo.CatalogItems ci ON ci.Sku = v.Sku
WHERE NOT EXISTS (SELECT 1 FROM dbo.CatalogItemDetails d WHERE d.CatalogItemId = ci.Id);
GO

-- Зв'язки M:N «позиція - категорія» з власними полями AddedAt та IsPrimary
INSERT INTO dbo.CatalogItemCategories (CatalogItemId, CategoryId, AddedAt, IsPrimary)
SELECT ci.Id, c.Id, v.AddedAt, v.IsPrimary
FROM (VALUES
    (N'PRT-DSP-IP13',    N'displays',          CAST('2026-09-01T09:00:00' AS DATETIME2(0)), 1),
    (N'PRT-DSP-IP13',    N'for-smartphones',   CAST('2026-09-01T09:00:00' AS DATETIME2(0)), 0),
    (N'PRT-BAT-IP13',    N'batteries',         CAST('2026-09-01T09:05:00' AS DATETIME2(0)), 1),
    (N'PRT-BAT-IP13',    N'for-smartphones',   CAST('2026-09-01T09:05:00' AS DATETIME2(0)), 0),
    (N'PRT-BAT-S22',     N'batteries',         CAST('2026-09-03T14:20:00' AS DATETIME2(0)), 1),
    (N'PRT-BAT-S22',     N'for-smartphones',   CAST('2026-09-03T14:20:00' AS DATETIME2(0)), 0),
    (N'PRT-SSD-NV2-500', N'storage',           CAST('2026-09-10T11:00:00' AS DATETIME2(0)), 1),
    (N'PRT-SSD-NV2-500', N'for-laptops',       CAST('2026-09-15T16:30:00' AS DATETIME2(0)), 0),
    (N'PRT-TP-MX4-4',    N'consumables',       CAST('2026-09-10T11:10:00' AS DATETIME2(0)), 1),
    (N'PRT-TP-MX4-4',    N'for-laptops',       CAST('2026-10-02T10:00:00' AS DATETIME2(0)), 0),
    (N'SRV-DIAG',        N'diagnostics',       CAST('2026-09-01T08:00:00' AS DATETIME2(0)), 1),
    (N'SRV-DSP-PHONE',   N'smartphone-repair', CAST('2026-09-01T08:10:00' AS DATETIME2(0)), 1),
    (N'SRV-BAT-PHONE',   N'smartphone-repair', CAST('2026-09-01T08:15:00' AS DATETIME2(0)), 1),
    (N'SRV-CLEAN-NB',    N'laptop-repair',     CAST('2026-09-01T08:20:00' AS DATETIME2(0)), 1),
    (N'SRV-SSD-MIGR',    N'laptop-repair',     CAST('2026-09-12T12:00:00' AS DATETIME2(0)), 1)
) AS v (Sku, CategorySlug, AddedAt, IsPrimary)
JOIN dbo.CatalogItems ci ON ci.Sku  = v.Sku
JOIN dbo.Categories   c  ON c.Slug  = v.CategorySlug
WHERE NOT EXISTS (SELECT 1 FROM dbo.CatalogItemCategories x
                  WHERE x.CatalogItemId = ci.Id AND x.CategoryId = c.Id);
GO

-- Контроль: кількість рядків
SELECT 'Brands' AS [Table], COUNT(*) AS [Rows] FROM dbo.Brands
UNION ALL SELECT 'Categories',            COUNT(*) FROM dbo.Categories
UNION ALL SELECT 'CatalogItems',          COUNT(*) FROM dbo.CatalogItems
UNION ALL SELECT 'CatalogItemDetails',    COUNT(*) FROM dbo.CatalogItemDetails
UNION ALL SELECT 'CatalogItemCategories', COUNT(*) FROM dbo.CatalogItemCategories;
GO

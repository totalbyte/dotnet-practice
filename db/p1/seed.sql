/* Проєкт №1 - RepairsDb. Тестові дані; запускати після procedures.sql.
   Ідемпотентно: кожна вставка перевіряє наявність запису (WHERE NOT EXISTS). */

USE RepairsDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

-- Клієнти
INSERT INTO dbo.Customers (FullName, Email, Phone, CreatedBy)
SELECT v.FullName, v.Email, v.Phone, N'seed'
FROM (VALUES
    (N'Олена Коваленко',  N'olena.kovalenko@example.com',  N'+380501112233'),
    (N'Андрій Шевченко',  N'andrii.shevchenko@example.com', N'+380672223344'),
    (N'Марія Бондаренко', N'maria.bondarenko@example.com',  N'+380933334455'),
    (N'Тарас Мельник',    N'taras.melnyk@example.com',      N'+380634445566')
) AS v (FullName, Email, Phone)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Customers c WHERE c.Email = v.Email);
GO

-- Локальна копія позицій Каталогу: Id збігаються з CatalogDb
INSERT INTO dbo.CatalogItems (Id, Name, ItemType, Price, Stock)
SELECT v.Id, v.Name, v.ItemType, v.Price, v.Stock
FROM (VALUES
    (1,  N'Екран OLED для iPhone 13',                 N'Part',    4200.00,  5),
    (2,  N'Акумулятор для iPhone 13',                 N'Part',    1450.00, 10),
    (3,  N'Акумулятор для Samsung Galaxy S22',        N'Part',    1250.00,  7),
    (4,  N'SSD Kingston NV2 500 ГБ',                  N'Part',    1890.00,  5),
    (5,  N'Термопаста Arctic MX-4, 4 г',              N'Part',     320.00, 19),
    (6,  N'Діагностика пристрою',                     N'Service',  300.00,  0),
    (7,  N'Заміна екрана смартфона',                  N'Service',  800.00,  0),
    (8,  N'Чистка ноутбука від пилу',                 N'Service',  650.00,  0),
    (9,  N'Заміна акумулятора смартфона',             N'Service',  450.00,  0),
    (10, N'Встановлення SSD з перенесенням даних',    N'Service',  700.00,  0)
) AS v (Id, Name, ItemType, Price, Stock)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CatalogItems ci WHERE ci.Id = v.Id);
GO

-- Замовлення на ремонт
SET IDENTITY_INSERT dbo.RepairOrders ON;

INSERT INTO dbo.RepairOrders (Id, CustomerId, Status, AcceptedAt, IssuedAt, CreatedAt, CreatedBy)
SELECT v.Id, c.Id, v.Status, v.AcceptedAt, v.IssuedAt, v.AcceptedAt, N'seed'
FROM (VALUES
    (1, N'olena.kovalenko@example.com',   N'Issued',    CAST('2026-10-01T09:15:00' AS DATETIME2(0)), CAST('2026-10-03T17:40:00' AS DATETIME2(0))),
    (2, N'andrii.shevchenko@example.com', N'Ready',     CAST('2026-10-20T11:00:00' AS DATETIME2(0)), NULL),
    (3, N'maria.bondarenko@example.com',  N'Diagnosed', CAST('2026-11-02T14:30:00' AS DATETIME2(0)), NULL),
    (4, N'olena.kovalenko@example.com',   N'Accepted',  CAST('2026-11-10T10:05:00' AS DATETIME2(0)), NULL),
    (5, N'taras.melnyk@example.com',      N'Cancelled', CAST('2026-10-12T16:20:00' AS DATETIME2(0)), NULL)
) AS v (Id, Email, Status, AcceptedAt, IssuedAt)
JOIN dbo.Customers c ON c.Email = v.Email
WHERE NOT EXISTS (SELECT 1 FROM dbo.RepairOrders o WHERE o.Id = v.Id);

SET IDENTITY_INSERT dbo.RepairOrders OFF;
GO

-- Деталі замовлень (1:1)
INSERT INTO dbo.RepairOrderDetails
       (RepairOrderId, DeviceType, DeviceBrand, DeviceModel, SerialNumber, DefectDescription, DiagnosticNotes)
SELECT v.RepairOrderId, v.DeviceType, v.DeviceBrand, v.DeviceModel, v.SerialNumber, v.DefectDescription, v.DiagnosticNotes
FROM (VALUES
    (1, N'Smartphone', N'Apple',   N'iPhone 13',       N'F2LXK1ABCD01', N'Розбитий екран після падіння, сенсор не реагує',
        N'Дисплейний модуль пошкоджено, корпус і плата без дефектів'),
    (2, N'Laptop',     N'Lenovo',  N'IdeaPad 5 15ALC', N'PF3ABC12',     N'Сильно гріється і шумить, вимикається під навантаженням',
        N'Радіатор забитий пилом, термопаста висохла'),
    (3, N'Smartphone', N'Samsung', N'Galaxy S22',      NULL,            N'Швидко розряджається, вимикається на 20%',
        N'Знос акумулятора 41%, рекомендовано заміну'),
    (4, N'Laptop',     N'ASUS',    N'VivoBook 15',     N'N4N0CX12345',  N'Не завантажується система, чутно клацання диска', NULL),
    (5, N'TV',         N'LG',      N'43UP7500',        NULL,            N'Немає зображення, звук є',
        N'Потрібна заміна матриці, вартість близька до ціни нового телевізора')
) AS v (RepairOrderId, DeviceType, DeviceBrand, DeviceModel, SerialNumber, DefectDescription, DiagnosticNotes)
WHERE EXISTS     (SELECT 1 FROM dbo.RepairOrders o WHERE o.Id = v.RepairOrderId)
  AND NOT EXISTS (SELECT 1 FROM dbo.RepairOrderDetails d WHERE d.RepairOrderId = v.RepairOrderId);
GO

-- Позиції замовлень (M:N). ItemName і UnitPrice - знімки: у замовленні 1 екран коштує 3900.00, у довіднику вже 4200.00
-- AddedAt - дата прийняття замовлення, а не час запуску seed
INSERT INTO dbo.RepairOrderItems (RepairOrderId, CatalogItemId, ItemName, UnitPrice, Quantity, Discount, AddedBy, AddedAt)
SELECT v.RepairOrderId, v.CatalogItemId, v.ItemName, v.UnitPrice, v.Quantity, v.Discount, N'seed', o.AcceptedAt
FROM (VALUES
    (1, 6, N'Діагностика пристрою',              300.00, 1, 300.00),
    (1, 1, N'Екран OLED для iPhone 13',         3900.00, 1,   0.00),
    (1, 7, N'Заміна екрана смартфона',           800.00, 1,   0.00),
    (2, 8, N'Чистка ноутбука від пилу',          650.00, 1,   0.00),
    (2, 5, N'Термопаста Arctic MX-4, 4 г',       320.00, 1,   0.00),
    (3, 6, N'Діагностика пристрою',              300.00, 1,   0.00),
    (3, 3, N'Акумулятор для Samsung Galaxy S22',1250.00, 1,   0.00),
    (3, 9, N'Заміна акумулятора смартфона',      450.00, 1,  50.00)
) AS v (RepairOrderId, CatalogItemId, ItemName, UnitPrice, Quantity, Discount)
JOIN dbo.RepairOrders o ON o.Id = v.RepairOrderId
WHERE NOT EXISTS (SELECT 1 FROM dbo.RepairOrderItems i
                  WHERE i.RepairOrderId = v.RepairOrderId AND i.CatalogItemId = v.CatalogItemId);
GO

-- Історія статусів (1:N)
INSERT INTO dbo.RepairOrderStatusHistory (RepairOrderId, FromStatus, ToStatus, Comment, ChangedAt, ChangedBy)
SELECT v.RepairOrderId, v.FromStatus, v.ToStatus, v.Comment, v.ChangedAt, v.ChangedBy
FROM (VALUES
    (1, NULL,         N'Accepted',  N'Пристрій прийнято в ремонт',          CAST('2026-10-01T09:15:00' AS DATETIME2(0)), N'reception'),
    (1, N'Accepted',  N'Diagnosed', N'Потрібна заміна дисплейного модуля',  CAST('2026-10-01T12:00:00' AS DATETIME2(0)), N'master.ivan'),
    (1, N'Diagnosed', N'Approved',  N'Клієнт погодив кошторис телефоном',   CAST('2026-10-01T13:10:00' AS DATETIME2(0)), N'reception'),
    (1, N'Approved',  N'InRepair',  NULL,                                   CAST('2026-10-02T10:00:00' AS DATETIME2(0)), N'master.ivan'),
    (1, N'InRepair',  N'Ready',     N'Екран замінено, тест пройдено',       CAST('2026-10-03T15:00:00' AS DATETIME2(0)), N'master.ivan'),
    (1, N'Ready',     N'Paid',      N'Оплата карткою',                      CAST('2026-10-03T17:30:00' AS DATETIME2(0)), N'cashier'),
    (1, N'Paid',      N'Issued',    NULL,                                   CAST('2026-10-03T17:40:00' AS DATETIME2(0)), N'reception'),
    (2, NULL,         N'Accepted',  N'Пристрій прийнято в ремонт',          CAST('2026-10-20T11:00:00' AS DATETIME2(0)), N'reception'),
    (2, N'Accepted',  N'Diagnosed', N'Перегрів через пил',                  CAST('2026-10-20T15:30:00' AS DATETIME2(0)), N'master.oleh'),
    (2, N'Diagnosed', N'Approved',  NULL,                                   CAST('2026-10-21T09:00:00' AS DATETIME2(0)), N'reception'),
    (2, N'Approved',  N'InRepair',  NULL,                                   CAST('2026-10-21T10:00:00' AS DATETIME2(0)), N'master.oleh'),
    (2, N'InRepair',  N'Ready',     N'Почищено, термопасту замінено',       CAST('2026-10-21T16:45:00' AS DATETIME2(0)), N'master.oleh'),
    (3, NULL,         N'Accepted',  N'Пристрій прийнято в ремонт',          CAST('2026-11-02T14:30:00' AS DATETIME2(0)), N'reception'),
    (3, N'Accepted',  N'Diagnosed', N'Акумулятор зношений',                 CAST('2026-11-03T11:20:00' AS DATETIME2(0)), N'master.ivan'),
    (4, NULL,         N'Accepted',  N'Пристрій прийнято в ремонт',          CAST('2026-11-10T10:05:00' AS DATETIME2(0)), N'reception'),
    (5, NULL,         N'Accepted',  N'Пристрій прийнято в ремонт',          CAST('2026-10-12T16:20:00' AS DATETIME2(0)), N'reception'),
    (5, N'Accepted',  N'Diagnosed', N'Несправна матриця',                   CAST('2026-10-13T12:00:00' AS DATETIME2(0)), N'master.oleh'),
    (5, N'Diagnosed', N'Cancelled', N'Клієнт відмовився: ремонт недоцільний', CAST('2026-10-13T18:00:00' AS DATETIME2(0)), N'reception')
) AS v (RepairOrderId, FromStatus, ToStatus, Comment, ChangedAt, ChangedBy)
WHERE EXISTS     (SELECT 1 FROM dbo.RepairOrders o WHERE o.Id = v.RepairOrderId)
  AND NOT EXISTS (SELECT 1 FROM dbo.RepairOrderStatusHistory h
                  WHERE h.RepairOrderId = v.RepairOrderId AND h.ToStatus = v.ToStatus);
GO

-- Контроль: кількість рядків
SELECT 'Customers' AS [Table], COUNT(*) AS [Rows] FROM dbo.Customers
UNION ALL SELECT 'CatalogItems',             COUNT(*) FROM dbo.CatalogItems
UNION ALL SELECT 'RepairOrders',             COUNT(*) FROM dbo.RepairOrders
UNION ALL SELECT 'RepairOrderDetails',       COUNT(*) FROM dbo.RepairOrderDetails
UNION ALL SELECT 'RepairOrderItems',         COUNT(*) FROM dbo.RepairOrderItems
UNION ALL SELECT 'RepairOrderStatusHistory', COUNT(*) FROM dbo.RepairOrderStatusHistory;
GO

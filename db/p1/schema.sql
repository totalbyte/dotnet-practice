/* Проєкт №1 - контекст Repairs, база RepairsDb. Таблиці, ключі, обмеження, індекси.
   Порядок запуску: schema.sql -> procedures.sql -> seed.sql. Скрипт ідемпотентний.
   Усі FOREIGN KEY - лише всередині RepairsDb; зовнішніх ключів на інші бази немає.
   PK - BIGINT IDENTITY: рядки вставляє одне джерело, послідовні значення не фрагментують індекс. */

IF DB_ID(N'RepairsDb') IS NULL
    CREATE DATABASE RepairsDb;
GO

USE RepairsDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Версії схеми: кожна міграція додає рядок
IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
CREATE TABLE dbo.SchemaVersions (
    Version      NVARCHAR(20)  NOT NULL CONSTRAINT PK_SchemaVersions PRIMARY KEY,
    Description  NVARCHAR(200) NOT NULL,
    AppliedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_SchemaVersions_AppliedAt DEFAULT SYSUTCDATETIME()
);
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
CREATE TABLE dbo.Customers (
    Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    FullName    NVARCHAR(200) NOT NULL,
    Email       NVARCHAR(256) NOT NULL,
    Phone       NVARCHAR(20)  NOT NULL,
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NULL,
    UpdatedAt   DATETIME2(0)  NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT           NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT 0,
    -- оптимістична конкурентність: картку клієнта можуть одночасно правити два приймальники
    RowVersion  ROWVERSION    NOT NULL,
    CONSTRAINT UQ_Customers_Email UNIQUE (Email),
    CONSTRAINT CK_Customers_Email CHECK (Email LIKE N'_%@_%._%'),
    CONSTRAINT CK_Customers_Phone CHECK (LEN(Phone) >= 10)
);
GO

-- 1:N Customers -> RepairOrders. FK без каскаду: клієнта з історією ремонтів видалити не можна
IF OBJECT_ID(N'dbo.RepairOrders', N'U') IS NULL
CREATE TABLE dbo.RepairOrders (
    Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RepairOrders PRIMARY KEY,
    CustomerId  BIGINT        NOT NULL
                CONSTRAINT FK_RepairOrders_Customers REFERENCES dbo.Customers(Id),
    Status      NVARCHAR(30)  NOT NULL CONSTRAINT DF_RepairOrders_Status DEFAULT N'Accepted',
    AcceptedAt  DATETIME2(0)  NOT NULL CONSTRAINT DF_RepairOrders_AcceptedAt DEFAULT SYSUTCDATETIME(),
    IssuedAt    DATETIME2(0)  NULL,
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_RepairOrders_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NULL,
    UpdatedAt   DATETIME2(0)  NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT           NOT NULL CONSTRAINT DF_RepairOrders_IsDeleted DEFAULT 0,
    -- оптимістична конкурентність: статус замовлення паралельно змінюють майстер і касир
    RowVersion  ROWVERSION    NOT NULL,
    CONSTRAINT CK_RepairOrders_Status CHECK (Status IN
        (N'Accepted', N'Diagnosed', N'Approved', N'InRepair', N'Ready', N'Paid', N'Issued', N'Cancelled')),
    CONSTRAINT CK_RepairOrders_IssuedAt CHECK (IssuedAt IS NULL OR IssuedAt >= AcceptedAt)
);
GO

-- 1:1 RepairOrders <-> RepairOrderDetails: PK є водночас FK. CASCADE: деталі не існують без замовлення
IF OBJECT_ID(N'dbo.RepairOrderDetails', N'U') IS NULL
CREATE TABLE dbo.RepairOrderDetails (
    RepairOrderId      BIGINT         NOT NULL
                       CONSTRAINT PK_RepairOrderDetails PRIMARY KEY
                       CONSTRAINT FK_RepairOrderDetails_RepairOrders
                           REFERENCES dbo.RepairOrders(Id) ON DELETE CASCADE,
    DeviceType         NVARCHAR(50)   NOT NULL,
    DeviceBrand        NVARCHAR(100)  NULL,
    DeviceModel        NVARCHAR(100)  NOT NULL,
    SerialNumber       NVARCHAR(100)  NULL,
    DefectDescription  NVARCHAR(1000) NOT NULL,
    DiagnosticNotes    NVARCHAR(2000) NULL,
    UpdatedAt          DATETIME2(0)   NULL,
    UpdatedBy          NVARCHAR(100)  NULL,
    -- оптимістична конкурентність: опис діагностики можуть одночасно редагувати два майстри
    RowVersion         ROWVERSION     NOT NULL,
    CONSTRAINT CK_RepairOrderDetails_DeviceType CHECK (DeviceType IN
        (N'Smartphone', N'Laptop', N'Tablet', N'Desktop', N'TV', N'Appliance', N'Other'))
);
GO

/* Локальна копія позицій контексту Catalog. Id - ідентифікатор із CatalogDb: без IDENTITY і без FK на іншу базу.
   Name, ItemType, Price - копія для читання, оновлюється подіями з Каталогу. Stock веде сам Repairs. */
IF OBJECT_ID(N'dbo.CatalogItems', N'U') IS NULL
CREATE TABLE dbo.CatalogItems (
    Id          BIGINT        NOT NULL CONSTRAINT PK_CatalogItems PRIMARY KEY,
    Name        NVARCHAR(200) NOT NULL,
    ItemType    NVARCHAR(20)  NOT NULL,
    Price       DECIMAL(18,2) NOT NULL,
    Stock       INT           NOT NULL CONSTRAINT DF_CatalogItems_Stock DEFAULT 0,
    IsActive    BIT           NOT NULL CONSTRAINT DF_CatalogItems_IsActive DEFAULT 1,
    SyncedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_CatalogItems_SyncedAt DEFAULT SYSUTCDATETIME(),
    -- оптимістична конкурентність: залишок одночасно списують кілька замовлень
    RowVersion  ROWVERSION    NOT NULL,
    CONSTRAINT CK_CatalogItems_ItemType CHECK (ItemType IN (N'Part', N'Service')),
    CONSTRAINT CK_CatalogItems_Price    CHECK (Price > 0),
    CONSTRAINT CK_CatalogItems_Stock    CHECK (Stock >= 0)
);
GO

/* M:N RepairOrders <-> CatalogItems: проміжна таблиця з двома FK і власними полями.
   ItemName, UnitPrice - знімок на момент додавання, не оновлюється.
   CASCADE від замовлення: позиції не існують без нього. NO ACTION від довідника: використану позицію видалити не можна. */
IF OBJECT_ID(N'dbo.RepairOrderItems', N'U') IS NULL
CREATE TABLE dbo.RepairOrderItems (
    RepairOrderId  BIGINT        NOT NULL
                   CONSTRAINT FK_RepairOrderItems_RepairOrders
                       REFERENCES dbo.RepairOrders(Id) ON DELETE CASCADE,
    CatalogItemId  BIGINT        NOT NULL
                   CONSTRAINT FK_RepairOrderItems_CatalogItems REFERENCES dbo.CatalogItems(Id),
    ItemName       NVARCHAR(200) NOT NULL,           -- знімок назви на момент додавання
    UnitPrice      DECIMAL(18,2) NOT NULL,           -- знімок ціни на момент додавання
    Quantity       INT           NOT NULL,
    Discount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_RepairOrderItems_Discount DEFAULT 0,
    LineTotal      AS (CONVERT(DECIMAL(18,2), UnitPrice * Quantity - Discount)) PERSISTED,
    AddedAt        DATETIME2(0)  NOT NULL CONSTRAINT DF_RepairOrderItems_AddedAt DEFAULT SYSUTCDATETIME(),
    AddedBy        NVARCHAR(100) NULL,
    CONSTRAINT PK_RepairOrderItems PRIMARY KEY (RepairOrderId, CatalogItemId),
    CONSTRAINT CK_RepairOrderItems_UnitPrice CHECK (UnitPrice > 0),
    CONSTRAINT CK_RepairOrderItems_Quantity  CHECK (Quantity > 0),
    CONSTRAINT CK_RepairOrderItems_Discount  CHECK (Discount >= 0 AND Discount <= UnitPrice * Quantity)
);
GO

-- 1:N RepairOrders -> RepairOrderStatusHistory
IF OBJECT_ID(N'dbo.RepairOrderStatusHistory', N'U') IS NULL
CREATE TABLE dbo.RepairOrderStatusHistory (
    Id             BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RepairOrderStatusHistory PRIMARY KEY,
    RepairOrderId  BIGINT        NOT NULL
                   CONSTRAINT FK_RepairOrderStatusHistory_RepairOrders
                       REFERENCES dbo.RepairOrders(Id) ON DELETE CASCADE,
    FromStatus     NVARCHAR(30)  NULL,
    ToStatus       NVARCHAR(30)  NOT NULL,
    Comment        NVARCHAR(500) NULL,
    ChangedAt      DATETIME2(0)  NOT NULL CONSTRAINT DF_RepairOrderStatusHistory_ChangedAt DEFAULT SYSUTCDATETIME(),
    ChangedBy      NVARCHAR(100) NULL,
    CONSTRAINT CK_RepairOrderStatusHistory_ToStatus CHECK (ToStatus IN
        (N'Accepted', N'Diagnosed', N'Approved', N'InRepair', N'Ready', N'Paid', N'Issued', N'Cancelled'))
);
GO

-- Запит: «активні замовлення клієнта з певним статусом» (кабінет клієнта)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RepairOrders_CustomerId_Status'
               AND object_id = OBJECT_ID(N'dbo.RepairOrders'))
    CREATE INDEX IX_RepairOrders_CustomerId_Status
        ON dbo.RepairOrders (CustomerId, Status)
        INCLUDE (AcceptedAt)
        WHERE IsDeleted = 0;
GO

-- Запит: «черга майстра» - усі замовлення в заданому статусі від найстаріших
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RepairOrders_Status_AcceptedAt'
               AND object_id = OBJECT_ID(N'dbo.RepairOrders'))
    CREATE INDEX IX_RepairOrders_Status_AcceptedAt
        ON dbo.RepairOrders (Status, AcceptedAt)
        INCLUDE (CustomerId)
        WHERE IsDeleted = 0;
GO

-- Запит: «пошук клієнта за номером телефону на рецепції»
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customers_Phone'
               AND object_id = OBJECT_ID(N'dbo.Customers'))
    CREATE INDEX IX_Customers_Phone
        ON dbo.Customers (Phone)
        INCLUDE (FullName, Email)
        WHERE IsDeleted = 0;
GO

-- Запит: «чи був цей пристрій у ремонті раніше» - пошук за серійним номером (гарантійні звернення)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RepairOrderDetails_SerialNumber'
               AND object_id = OBJECT_ID(N'dbo.RepairOrderDetails'))
    CREATE INDEX IX_RepairOrderDetails_SerialNumber
        ON dbo.RepairOrderDetails (SerialNumber)
        WHERE SerialNumber IS NOT NULL;
GO

-- Запит: «у яких замовленнях використано запчастину і скільки її списано» (звіт по складу)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RepairOrderItems_CatalogItemId'
               AND object_id = OBJECT_ID(N'dbo.RepairOrderItems'))
    CREATE INDEX IX_RepairOrderItems_CatalogItemId
        ON dbo.RepairOrderItems (CatalogItemId)
        INCLUDE (Quantity);
GO

-- Запит: «хронологія статусів одного замовлення» (третій набір результатів usp_GetRepairOrderById)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RepairOrderStatusHistory_RepairOrderId_ChangedAt'
               AND object_id = OBJECT_ID(N'dbo.RepairOrderStatusHistory'))
    CREATE INDEX IX_RepairOrderStatusHistory_RepairOrderId_ChangedAt
        ON dbo.RepairOrderStatusHistory (RepairOrderId, ChangedAt);
GO

-- Денормалізоване представлення для списку замовлень
CREATE OR ALTER VIEW dbo.vw_RepairOrderSummary
AS
SELECT  o.Id                AS RepairOrderId,
        o.Status,
        o.AcceptedAt,
        o.IssuedAt,
        c.Id                AS CustomerId,
        c.FullName          AS CustomerName,
        c.Phone             AS CustomerPhone,
        d.DeviceType,
        d.DeviceBrand,
        d.DeviceModel,
        d.SerialNumber,
        ISNULL(t.ItemsCount, 0)   AS ItemsCount,
        ISNULL(t.TotalAmount, 0)  AS TotalAmount
FROM dbo.RepairOrders o
JOIN dbo.Customers c               ON c.Id = o.CustomerId
LEFT JOIN dbo.RepairOrderDetails d ON d.RepairOrderId = o.Id
OUTER APPLY (SELECT COUNT(*) AS ItemsCount, SUM(i.LineTotal) AS TotalAmount
             FROM dbo.RepairOrderItems i
             WHERE i.RepairOrderId = o.Id) t
WHERE o.IsDeleted = 0;
GO

-- Роль застосунку: найменші привілеї, без фізичного видалення і змін схеми
IF DATABASE_PRINCIPAL_ID(N'repairs_app') IS NULL
    CREATE ROLE repairs_app;
GO
GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo TO repairs_app;
GRANT EXECUTE ON SCHEMA::dbo TO repairs_app;
DENY  DELETE ON SCHEMA::dbo TO repairs_app;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'1.0.0')
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'1.0.0', N'Initial schema: customers, repair orders, details, items, status history');
GO

PRINT N'RepairsDb: schema is up to date';
GO

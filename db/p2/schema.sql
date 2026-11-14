/* Проєкт №2 - контекст Catalog, база CatalogDb. Цільова схема для EF Core Code First.
   Порядок запуску: schema.sql -> seed.sql. Скрипт ідемпотентний.
   Усі FOREIGN KEY - лише всередині CatalogDb. Імена ключів та індексів відповідають угодам EF Core.
   PK: CatalogItems - BIGINT IDENTITY (Id передається в інші контексти); довідники - INT IDENTITY. */

IF DB_ID(N'CatalogDb') IS NULL
    CREATE DATABASE CatalogDb;
GO

USE CatalogDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Версії схеми (службова таблиця, не сутність домену)
IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
CREATE TABLE dbo.SchemaVersions (
    Version      NVARCHAR(20)  NOT NULL CONSTRAINT PK_SchemaVersions PRIMARY KEY,
    Description  NVARCHAR(200) NOT NULL,
    AppliedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_SchemaVersions_AppliedAt DEFAULT SYSUTCDATETIME()
);
GO

IF OBJECT_ID(N'dbo.Brands', N'U') IS NULL
CREATE TABLE dbo.Brands (
    Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Brands PRIMARY KEY,
    Name       NVARCHAR(100) NOT NULL,
    Country    NVARCHAR(100) NULL,
    CreatedAt  DATETIME2(0)  NOT NULL CONSTRAINT DF_Brands_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Brands_Name UNIQUE (Name)
);
GO

-- 1:N Categories -> Categories (ієрархія). Без каскаду: категорію з підкатегоріями видалити не можна
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories (
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name              NVARCHAR(100) NOT NULL,
    Slug              NVARCHAR(100) NOT NULL,
    ParentCategoryId  INT           NULL
                      CONSTRAINT FK_Categories_Categories_ParentCategoryId REFERENCES dbo.Categories(Id),
    DisplayOrder      INT           NOT NULL CONSTRAINT DF_Categories_DisplayOrder DEFAULT 0,
    CreatedAt         DATETIME2(0)  NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Categories_Name UNIQUE (Name),
    CONSTRAINT UQ_Categories_Slug UNIQUE (Slug),
    CONSTRAINT CK_Categories_Slug         CHECK (LEN(Slug) > 0 AND Slug NOT LIKE N'% %'),
    CONSTRAINT CK_Categories_DisplayOrder CHECK (DisplayOrder >= 0),
    CONSTRAINT CK_Categories_NotOwnParent CHECK (ParentCategoryId IS NULL OR ParentCategoryId <> Id)
);
GO

-- 1:N Brands -> CatalogItems. BrandId необов'язковий: у послуг бренду немає. Без каскаду: бренд із позиціями видалити не можна
IF OBJECT_ID(N'dbo.CatalogItems', N'U') IS NULL
CREATE TABLE dbo.CatalogItems (
    Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CatalogItems PRIMARY KEY,
    Sku         NVARCHAR(50)  NOT NULL,
    Name        NVARCHAR(200) NOT NULL,
    ItemType    NVARCHAR(20)  NOT NULL,
    Price       DECIMAL(18,2) NOT NULL,
    BrandId     INT           NULL
                CONSTRAINT FK_CatalogItems_Brands_BrandId REFERENCES dbo.Brands(Id),
    IsActive    BIT           NOT NULL CONSTRAINT DF_CatalogItems_IsActive DEFAULT 1,
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_CatalogItems_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NULL,
    UpdatedAt   DATETIME2(0)  NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT           NOT NULL CONSTRAINT DF_CatalogItems_IsDeleted DEFAULT 0,
    -- оптимістична конкурентність: ціну можуть одночасно редагувати два менеджери
    RowVersion  ROWVERSION    NOT NULL,
    CONSTRAINT UQ_CatalogItems_Sku UNIQUE (Sku),
    CONSTRAINT CK_CatalogItems_ItemType CHECK (ItemType IN (N'Part', N'Service')),
    CONSTRAINT CK_CatalogItems_Price    CHECK (Price > 0),
    CONSTRAINT CK_CatalogItems_ServiceHasNoBrand CHECK (ItemType = N'Part' OR BrandId IS NULL)
);
GO

-- 1:1 CatalogItems <-> CatalogItemDetails: PK є водночас FK. CASCADE: опис не існує без позиції
IF OBJECT_ID(N'dbo.CatalogItemDetails', N'U') IS NULL
CREATE TABLE dbo.CatalogItemDetails (
    CatalogItemId             BIGINT         NOT NULL
                              CONSTRAINT PK_CatalogItemDetails PRIMARY KEY
                              CONSTRAINT FK_CatalogItemDetails_CatalogItems_CatalogItemId
                                  REFERENCES dbo.CatalogItems(Id) ON DELETE CASCADE,
    Description               NVARCHAR(2000) NULL,
    WarrantyMonths            INT            NOT NULL CONSTRAINT DF_CatalogItemDetails_WarrantyMonths DEFAULT 0,
    EstimatedDurationMinutes  INT            NULL,
    Specifications            NVARCHAR(MAX)  NULL,
    CONSTRAINT CK_CatalogItemDetails_WarrantyMonths CHECK (WarrantyMonths BETWEEN 0 AND 60),
    CONSTRAINT CK_CatalogItemDetails_Duration       CHECK (EstimatedDurationMinutes IS NULL OR EstimatedDurationMinutes > 0),
    CONSTRAINT CK_CatalogItemDetails_Specifications CHECK (Specifications IS NULL OR ISJSON(Specifications) = 1)
);
GO

/* M:N CatalogItems <-> Categories: проміжна сутність з власними полями AddedAt та IsPrimary.
   Обидва FK каскадні: зв'язок не має сенсу без будь-якої зі сторін. */
IF OBJECT_ID(N'dbo.CatalogItemCategories', N'U') IS NULL
CREATE TABLE dbo.CatalogItemCategories (
    CatalogItemId  BIGINT       NOT NULL
                   CONSTRAINT FK_CatalogItemCategories_CatalogItems_CatalogItemId
                       REFERENCES dbo.CatalogItems(Id) ON DELETE CASCADE,
    CategoryId     INT          NOT NULL
                   CONSTRAINT FK_CatalogItemCategories_Categories_CategoryId
                       REFERENCES dbo.Categories(Id) ON DELETE CASCADE,
    AddedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_CatalogItemCategories_AddedAt DEFAULT SYSUTCDATETIME(),
    IsPrimary      BIT          NOT NULL CONSTRAINT DF_CatalogItemCategories_IsPrimary DEFAULT 0,
    CONSTRAINT PK_CatalogItemCategories PRIMARY KEY (CatalogItemId, CategoryId)
);
GO

-- Запит: «пошук позиції за початком назви» у рядку пошуку каталогу
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CatalogItems_Name'
               AND object_id = OBJECT_ID(N'dbo.CatalogItems'))
    CREATE INDEX IX_CatalogItems_Name
        ON dbo.CatalogItems (Name)
        INCLUDE (Sku, Price, ItemType)
        WHERE IsDeleted = 0;
GO

-- Запит: «активні позиції заданого типу, відсортовані за ціною» (фільтр «Запчастини / Послуги»)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CatalogItems_ItemType_Price'
               AND object_id = OBJECT_ID(N'dbo.CatalogItems'))
    CREATE INDEX IX_CatalogItems_ItemType_Price
        ON dbo.CatalogItems (ItemType, Price)
        INCLUDE (Name, Sku)
        WHERE IsActive = 1 AND IsDeleted = 0;
GO

-- Запит: «усі позиції бренду» (сторінка бренду); також прискорює перевірку FK при видаленні бренду
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CatalogItems_BrandId'
               AND object_id = OBJECT_ID(N'dbo.CatalogItems'))
    CREATE INDEX IX_CatalogItems_BrandId
        ON dbo.CatalogItems (BrandId)
        WHERE BrandId IS NOT NULL AND IsDeleted = 0;
GO

-- Запит: «позиції категорії, спочатку нещодавно додані» (сторінка категорії). PK починається
-- з CatalogItemId, тому пошук від боку категорії потребує власного індексу
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CatalogItemCategories_CategoryId_AddedAt'
               AND object_id = OBJECT_ID(N'dbo.CatalogItemCategories'))
    CREATE INDEX IX_CatalogItemCategories_CategoryId_AddedAt
        ON dbo.CatalogItemCategories (CategoryId, AddedAt DESC);
GO

-- Правило + запит: у позиції може бути лише ОДНА основна категорія; індекс заодно обслуговує
-- запит «основна категорія позиції» для хлібних крихт
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CatalogItemCategories_CatalogItemId_Primary'
               AND object_id = OBJECT_ID(N'dbo.CatalogItemCategories'))
    CREATE UNIQUE INDEX UX_CatalogItemCategories_CatalogItemId_Primary
        ON dbo.CatalogItemCategories (CatalogItemId)
        INCLUDE (CategoryId)
        WHERE IsPrimary = 1;
GO

-- Запит: «підкатегорії заданої категорії в порядку показу» (дерево меню)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_ParentCategoryId_DisplayOrder'
               AND object_id = OBJECT_ID(N'dbo.Categories'))
    CREATE INDEX IX_Categories_ParentCategoryId_DisplayOrder
        ON dbo.Categories (ParentCategoryId, DisplayOrder)
        INCLUDE (Name, Slug);
GO

-- Картка позиції для списку каталогу
CREATE OR ALTER VIEW dbo.vw_CatalogItemCards
AS
SELECT  ci.Id, ci.Sku, ci.Name, ci.ItemType, ci.Price, ci.IsActive,
        b.Name          AS BrandName,
        pc.Name         AS PrimaryCategoryName,
        d.WarrantyMonths,
        d.EstimatedDurationMinutes
FROM dbo.CatalogItems ci
LEFT JOIN dbo.Brands b                 ON b.Id = ci.BrandId
LEFT JOIN dbo.CatalogItemDetails d     ON d.CatalogItemId = ci.Id
LEFT JOIN dbo.CatalogItemCategories cc ON cc.CatalogItemId = ci.Id AND cc.IsPrimary = 1
LEFT JOIN dbo.Categories pc            ON pc.Id = cc.CategoryId
WHERE ci.IsDeleted = 0;
GO

-- Роль застосунку: найменші привілеї, без змін схеми
IF DATABASE_PRINCIPAL_ID(N'catalog_app') IS NULL
    CREATE ROLE catalog_app;
GO
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO catalog_app;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'1.0.0')
    INSERT INTO dbo.SchemaVersions (Version, Description)
    VALUES (N'1.0.0', N'Initial schema: brands, categories, catalog items, details, item-category links');
GO

PRINT N'CatalogDb: schema is up to date';
GO

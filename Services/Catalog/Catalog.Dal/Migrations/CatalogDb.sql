IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE TABLE [Brands] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Country] nvarchar(100) NULL,
        [CreatedAt] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [ParentCategoryId] int NULL,
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Categories_DisplayOrder] CHECK ([DisplayOrder] >= 0),
        CONSTRAINT [CK_Categories_NotOwnParent] CHECK ([ParentCategoryId] IS NULL OR [ParentCategoryId] <> [Id]),
        CONSTRAINT [CK_Categories_Slug] CHECK (LEN([Slug]) > 0 AND [Slug] NOT LIKE N'% %'),
        CONSTRAINT [FK_Categories_Categories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE TABLE [CatalogItems] (
        [Id] bigint NOT NULL IDENTITY,
        [Sku] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ItemType] nvarchar(20) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [BrandId] int NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(0) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CatalogItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CatalogItems_ItemType] CHECK ([ItemType] IN (N'Part', N'Service')),
        CONSTRAINT [CK_CatalogItems_Price] CHECK ([Price] > 0),
        CONSTRAINT [CK_CatalogItems_ServiceHasNoBrand] CHECK ([ItemType] = N'Part' OR [BrandId] IS NULL),
        CONSTRAINT [FK_CatalogItems_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE TABLE [CatalogItemCategories] (
        [CatalogItemId] bigint NOT NULL,
        [CategoryId] int NOT NULL,
        [AddedAt] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsPrimary] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_CatalogItemCategories] PRIMARY KEY ([CatalogItemId], [CategoryId]),
        CONSTRAINT [FK_CatalogItemCategories_CatalogItems_CatalogItemId] FOREIGN KEY ([CatalogItemId]) REFERENCES [CatalogItems] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CatalogItemCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE TABLE [CatalogItemDetails] (
        [CatalogItemId] bigint NOT NULL,
        [Description] nvarchar(2000) NULL,
        [WarrantyMonths] int NOT NULL DEFAULT 0,
        [EstimatedDurationMinutes] int NULL,
        [Specifications] nvarchar(max) NULL,
        CONSTRAINT [PK_CatalogItemDetails] PRIMARY KEY ([CatalogItemId]),
        CONSTRAINT [CK_CatalogItemDetails_Duration] CHECK ([EstimatedDurationMinutes] IS NULL OR [EstimatedDurationMinutes] > 0),
        CONSTRAINT [CK_CatalogItemDetails_Specifications] CHECK ([Specifications] IS NULL OR ISJSON([Specifications]) = 1),
        CONSTRAINT [CK_CatalogItemDetails_WarrantyMonths] CHECK ([WarrantyMonths] BETWEEN 0 AND 60),
        CONSTRAINT [FK_CatalogItemDetails_CatalogItems_CatalogItemId] FOREIGN KEY ([CatalogItemId]) REFERENCES [CatalogItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Country', N'CreatedAt', N'Name') AND [object_id] = OBJECT_ID(N'[Brands]'))
        SET IDENTITY_INSERT [Brands] ON;
    EXEC(N'INSERT INTO [Brands] ([Id], [Country], [CreatedAt], [Name])
    VALUES (1, N''США'', ''2026-09-01T00:00:00Z'', N''Apple''),
    (2, N''Південна Корея'', ''2026-09-01T00:00:00Z'', N''Samsung''),
    (3, N''США'', ''2026-09-01T00:00:00Z'', N''Kingston''),
    (4, N''Швейцарія'', ''2026-09-01T00:00:00Z'', N''Arctic''),
    (5, N''Китай'', ''2026-09-01T00:00:00Z'', N''Lenovo'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Country', N'CreatedAt', N'Name') AND [object_id] = OBJECT_ID(N'[Brands]'))
        SET IDENTITY_INSERT [Brands] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DisplayOrder', N'Name', N'ParentCategoryId', N'Slug') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([Id], [CreatedAt], [DisplayOrder], [Name], [ParentCategoryId], [Slug])
    VALUES (1, ''2026-09-01T00:00:00Z'', 1, N''Запчастини'', NULL, N''parts''),
    (2, ''2026-09-01T00:00:00Z'', 2, N''Послуги'', NULL, N''services''),
    (3, ''2026-09-01T00:00:00Z'', 1, N''Екрани та дисплеї'', 1, N''displays''),
    (4, ''2026-09-01T00:00:00Z'', 2, N''Акумулятори'', 1, N''batteries''),
    (5, ''2026-09-01T00:00:00Z'', 3, N''Накопичувачі'', 1, N''storage''),
    (6, ''2026-09-01T00:00:00Z'', 4, N''Витратні матеріали'', 1, N''consumables''),
    (7, ''2026-09-01T00:00:00Z'', 5, N''Для смартфонів'', 1, N''for-smartphones''),
    (8, ''2026-09-01T00:00:00Z'', 6, N''Для ноутбуків'', 1, N''for-laptops''),
    (9, ''2026-09-01T00:00:00Z'', 1, N''Діагностика'', 2, N''diagnostics''),
    (10, ''2026-09-01T00:00:00Z'', 2, N''Ремонт смартфонів'', 2, N''smartphone-repair''),
    (11, ''2026-09-01T00:00:00Z'', 3, N''Ремонт ноутбуків'', 2, N''laptop-repair'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DisplayOrder', N'Name', N'ParentCategoryId', N'Slug') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Brands_Name] ON [Brands] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CatalogItemCategories_CategoryId_AddedAt] ON [CatalogItemCategories] ([CategoryId], [AddedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CatalogItemCategories_CatalogItemId_Primary] ON [CatalogItemCategories] ([CatalogItemId]) INCLUDE ([CategoryId]) WHERE [IsPrimary] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_CatalogItems_BrandId] ON [CatalogItems] ([BrandId]) WHERE [BrandId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_CatalogItems_ItemType_Price] ON [CatalogItems] ([ItemType], [Price]) INCLUDE ([Name], [Sku]) WHERE [IsActive] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_CatalogItems_Name] ON [CatalogItems] ([Name]) INCLUDE ([Sku], [Price], [ItemType]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_CatalogItems_Sku] ON [CatalogItems] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Categories_ParentCategoryId_DisplayOrder] ON [Categories] ([ParentCategoryId], [DisplayOrder]) INCLUDE ([Name], [Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Categories_Slug] ON [Categories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115001627_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115001627_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115061336_AddAppRole'
)
BEGIN
    IF DATABASE_PRINCIPAL_ID(N'catalog_app') IS NULL
        CREATE ROLE [catalog_app];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115061336_AddAppRole'
)
BEGIN
    GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [catalog_app];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115061336_AddAppRole'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115061336_AddAppRole', N'10.0.15');
END;

COMMIT;
GO


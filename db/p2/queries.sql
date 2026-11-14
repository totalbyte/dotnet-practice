/* Проєкт №2 - CatalogDb. Запити перевірки; запускати після seed.sql.
   Дані не змінюються: некоректні вставки відхиляються обмеженнями. */

USE CatalogDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

PRINT N'--- 1. READ: картки позицій каталогу (view) ---';
SELECT Id, Sku, Name, ItemType, Price, BrandName, PrimaryCategoryName
FROM dbo.vw_CatalogItemCards
ORDER BY Id;
GO

PRINT N'--- 2. M:N: позиції категорії з датою додавання, новіші спочатку (IX_CatalogItemCategories_CategoryId_AddedAt) ---';
SELECT ci.Name, cc.AddedAt, cc.IsPrimary
FROM dbo.CatalogItemCategories cc
JOIN dbo.Categories c    ON c.Id  = cc.CategoryId
JOIN dbo.CatalogItems ci ON ci.Id = cc.CatalogItemId
WHERE c.Slug = N'for-smartphones'
ORDER BY cc.AddedAt DESC, ci.Id;
GO

PRINT N'--- 3. ПОШУК І ПАГІНАЦІЯ: активні запчастини за ціною, сторінка 1 по 3 (IX_CatalogItems_ItemType_Price) ---';
SELECT Sku, Name, Price
FROM dbo.CatalogItems
WHERE ItemType = N'Part' AND IsActive = 1 AND IsDeleted = 0
ORDER BY Price, Id
OFFSET 0 ROWS FETCH NEXT 3 ROWS ONLY;
GO

PRINT N'--- 4. ІЄРАРХІЯ 1:N: підкатегорії в порядку показу ---';
SELECT p.Name AS Parent, c.Name AS Category, c.Slug
FROM dbo.Categories c
JOIN dbo.Categories p ON p.Id = c.ParentCategoryId
ORDER BY p.DisplayOrder, c.DisplayOrder;
GO

PRINT N'--- 5. ОБМЕЖЕННЯ: CHECK, UNIQUE і унікальний фільтрований індекс відхиляють некоректні дані ---';
BEGIN TRY
    INSERT INTO dbo.CatalogItems (Sku, Name, ItemType, Price) VALUES (N'TEST-PRICE', N'Тест', N'Part', 0);
END TRY
BEGIN CATCH SELECT N'CHECK'  AS Constraint_, ERROR_MESSAGE() AS ErrorMessage; END CATCH
GO
BEGIN TRY
    INSERT INTO dbo.CatalogItems (Sku, Name, ItemType, Price) VALUES (N'SRV-DIAG', N'Дублікат', N'Service', 100);
END TRY
BEGIN CATCH SELECT N'UNIQUE' AS Constraint_, ERROR_MESSAGE() AS ErrorMessage; END CATCH
GO
BEGIN TRY
    UPDATE dbo.CatalogItemCategories SET IsPrimary = 1 WHERE CatalogItemId = 1;
END TRY
BEGIN CATCH SELECT N'UNIQUE INDEX' AS Constraint_, ERROR_MESSAGE() AS ErrorMessage; END CATCH
GO

PRINT N'--- 6. МЕЖІ КОНТЕКСТУ: усі зовнішні ключі посилаються лише на таблиці цієї ж бази ---';
SELECT fk.name AS ForeignKey,
       OBJECT_NAME(fk.parent_object_id)     AS FromTable,
       OBJECT_NAME(fk.referenced_object_id) AS ToTable,
       fk.delete_referential_action_desc    AS OnDelete
FROM sys.foreign_keys fk
ORDER BY FromTable, fk.name;
GO

PRINT N'--- 7. ІНДЕКСИ: перелік із фільтрами ---';
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name AS IndexName, i.type_desc AS IndexType,
       i.is_unique AS IsUnique, i.filter_definition AS Filter
FROM sys.indexes i
WHERE OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1 AND i.name IS NOT NULL
ORDER BY TableName, IndexName;
GO

PRINT N'--- 8. КОНТРОЛЬ: після всіх сценаріїв дані не змінилися ---';
SELECT (SELECT COUNT(*) FROM dbo.CatalogItems)          AS CatalogItems,
       (SELECT COUNT(*) FROM dbo.CatalogItemCategories) AS ItemCategoryLinks,
       (SELECT COUNT(*) FROM dbo.CatalogItemCategories WHERE IsPrimary = 1) AS PrimaryLinks;
GO

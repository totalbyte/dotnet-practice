/* Проєкт №1 - RepairsDb. Запити перевірки; запускати після seed.sql.
   Дані не змінюються: сценарії із записом відкочуються (ROLLBACK). */

USE RepairsDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

PRINT N'--- 1. READ: денормалізований список замовлень (view) ---';
SELECT RepairOrderId, Status, CustomerName, DeviceBrand, DeviceModel, ItemsCount, TotalAmount
FROM dbo.vw_RepairOrderSummary
ORDER BY RepairOrderId;
GO

PRINT N'--- 2. ПОШУК: активні замовлення клієнта з певним статусом (IX_RepairOrders_CustomerId_Status) ---';
DECLARE @CustomerId BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'olena.kovalenko@example.com');
SELECT Id, Status, AcceptedAt
FROM dbo.RepairOrders
WHERE CustomerId = @CustomerId AND Status = N'Accepted' AND IsDeleted = 0;
GO

PRINT N'--- 3. ПОШУК: клієнт за телефоном (IX_Customers_Phone) і пристрій за серійним номером ---';
SELECT Id, FullName, Email FROM dbo.Customers WHERE Phone = N'+380672223344' AND IsDeleted = 0;
SELECT RepairOrderId, DeviceModel FROM dbo.RepairOrderDetails WHERE SerialNumber = N'F2LXK1ABCD01';
GO

PRINT N'--- 4. ПАГІНАЦІЯ: сторінка 2 по 2 замовлення, новіші спочатку (OFFSET / FETCH) ---';
DECLARE @PageNumber INT = 2, @PageSize INT = 2;
SELECT RepairOrderId, Status, AcceptedAt, CustomerName
FROM dbo.vw_RepairOrderSummary
ORDER BY AcceptedAt DESC, RepairOrderId DESC
OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
GO

PRINT N'--- 5. READ через процедуру: агрегат «Замовлення» (3 набори результатів) ---';
EXEC dbo.usp_GetRepairOrderById @RepairOrderId = 1;
GO

PRINT N'--- 6. ЗНІМОК: ціна в позиції замовлення не змінилась, хоча в довіднику вона вже інша ---';
SELECT i.RepairOrderId, i.ItemName, i.UnitPrice AS SnapshotPrice, ci.Price AS CurrentCatalogPrice
FROM dbo.RepairOrderItems i
JOIN dbo.CatalogItems ci ON ci.Id = i.CatalogItemId
WHERE i.UnitPrice <> ci.Price;
GO

PRINT N'--- 7. ТРАНЗАКЦІЙНИЙ СЦЕНАРІЙ: повний цикл нового замовлення (CREATE + UPDATE), потім ROLLBACK ---';
BEGIN TRANSACTION;
    DECLARE @NewId BIGINT;
    DECLARE @Customer BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'taras.melnyk@example.com');

    EXEC dbo.usp_CreateRepairOrder
         @CustomerId = @Customer, @DeviceType = N'Laptop', @DeviceBrand = N'HP', @DeviceModel = N'Pavilion 15',
         @SerialNumber = N'5CD1234XYZ', @DefectDescription = N'Потрібно замінити диск на SSD',
         @CreatedBy = N'reception', @RepairOrderId = @NewId OUTPUT;

    EXEC dbo.usp_AddRepairOrderItem @RepairOrderId = @NewId, @CatalogItemId = 4,  @Quantity = 1, @AddedBy = N'master.oleh';
    EXEC dbo.usp_AddRepairOrderItem @RepairOrderId = @NewId, @CatalogItemId = 10, @Quantity = 1, @AddedBy = N'master.oleh';

    EXEC dbo.usp_ChangeRepairOrderStatus @RepairOrderId = @NewId, @NewStatus = N'Diagnosed', @ChangedBy = N'master.oleh';
    EXEC dbo.usp_ChangeRepairOrderStatus @RepairOrderId = @NewId, @NewStatus = N'Approved',  @ChangedBy = N'reception';
    EXEC dbo.usp_ChangeRepairOrderStatus @RepairOrderId = @NewId, @NewStatus = N'InRepair',  @ChangedBy = N'master.oleh';
    EXEC dbo.usp_ChangeRepairOrderStatus @RepairOrderId = @NewId, @NewStatus = N'Ready',     @ChangedBy = N'master.oleh';
    EXEC dbo.usp_ChangeRepairOrderStatus @RepairOrderId = @NewId, @NewStatus = N'Paid',      @ChangedBy = N'cashier';
    EXEC dbo.usp_ChangeRepairOrderStatus @RepairOrderId = @NewId, @NewStatus = N'Issued',    @ChangedBy = N'reception';

    SELECT RepairOrderId, Status, ItemsCount, TotalAmount FROM dbo.vw_RepairOrderSummary WHERE RepairOrderId = @NewId;
    SELECT Id, Name, Stock AS StockAfterWriteOff FROM dbo.CatalogItems WHERE Id = 4;

    EXEC dbo.usp_SoftDeleteRepairOrder @RepairOrderId = @NewId, @DeletedBy = N'admin';
    SELECT Id, Status, IsDeleted FROM dbo.RepairOrders WHERE Id = @NewId;
ROLLBACK TRANSACTION;
GO

PRINT N'--- 8. НЕДОПУСТИМИЙ ПЕРЕХІД: видати неоплачене замовлення (Ready -> Issued) ---';
BEGIN TRY
    EXEC dbo.usp_ChangeRepairOrderStatus @RepairOrderId = 2, @NewStatus = N'Issued', @ChangedBy = N'reception';
    SELECT N'ПОМИЛКА ТЕСТУ: перехід виконано' AS Result;
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage,
           (SELECT Status FROM dbo.RepairOrders WHERE Id = 2) AS StatusAfter;
END CATCH
GO

PRINT N'--- 9. НЕДОПУСТИМИЙ ПЕРЕХІД: скасувати замовлення після початку ремонту (Ready -> Cancelled) ---';
BEGIN TRY
    EXEC dbo.usp_ChangeRepairOrderStatus @RepairOrderId = 2, @NewStatus = N'Cancelled';
    SELECT N'ПОМИЛКА ТЕСТУ: перехід виконано' AS Result;
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage;
END CATCH
GO

PRINT N'--- 10. БІЗНЕС-ПРАВИЛА: порожній кошторис (50003), брак залишку (50007), видалення активного (50009) ---';
BEGIN TRY
    BEGIN TRANSACTION;
    EXEC dbo.usp_ChangeRepairOrderStatus @RepairOrderId = 4, @NewStatus = N'Diagnosed';
    EXEC dbo.usp_ChangeRepairOrderStatus @RepairOrderId = 4, @NewStatus = N'Approved';
    ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage;
END CATCH
GO
BEGIN TRY
    EXEC dbo.usp_AddRepairOrderItem @RepairOrderId = 4, @CatalogItemId = 1, @Quantity = 999;
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage;
END CATCH
GO
BEGIN TRY
    EXEC dbo.usp_SoftDeleteRepairOrder @RepairOrderId = 3;
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage;
END CATCH
GO

PRINT N'--- 11. ОПТИМІСТИЧНА КОНКУРЕНТНІСТЬ: другий запис зі старим RowVersion відхиляється (50008) ---';
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @StaleVersion BINARY(8) = (SELECT RowVersion FROM dbo.RepairOrderDetails WHERE RepairOrderId = 3);

    EXEC dbo.usp_UpdateRepairOrderDetails @RepairOrderId = 3,
         @DefectDescription = N'Швидко розряджається', @DiagnosticNotes = N'Правка майстра 1',
         @RowVersion = @StaleVersion, @UpdatedBy = N'master.ivan';

    EXEC dbo.usp_UpdateRepairOrderDetails @RepairOrderId = 3,
         @DefectDescription = N'Швидко розряджається', @DiagnosticNotes = N'Правка майстра 2',
         @RowVersion = @StaleVersion, @UpdatedBy = N'master.oleh';
    ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage;
END CATCH
GO

PRINT N'--- 12. ОБМЕЖЕННЯ: CHECK, UNIQUE і FK відхиляють некоректні дані ---';
BEGIN TRY
    INSERT INTO dbo.CatalogItems (Id, Name, ItemType, Price) VALUES (999, N'Тест', N'Part', -5);
END TRY
BEGIN CATCH SELECT N'CHECK'  AS Constraint_, ERROR_MESSAGE() AS ErrorMessage; END CATCH
GO
BEGIN TRY
    INSERT INTO dbo.Customers (FullName, Email, Phone) VALUES (N'Дублікат', N'olena.kovalenko@example.com', N'+380000000000');
END TRY
BEGIN CATCH SELECT N'UNIQUE' AS Constraint_, ERROR_MESSAGE() AS ErrorMessage; END CATCH
GO
BEGIN TRY
    DELETE FROM dbo.Customers WHERE Email = N'olena.kovalenko@example.com';
END TRY
BEGIN CATCH SELECT N'FK'     AS Constraint_, ERROR_MESSAGE() AS ErrorMessage; END CATCH
GO

PRINT N'--- 13. МЕЖІ КОНТЕКСТУ: усі зовнішні ключі посилаються лише на таблиці цієї ж бази ---';
SELECT fk.name AS ForeignKey,
       OBJECT_NAME(fk.parent_object_id)     AS FromTable,
       OBJECT_NAME(fk.referenced_object_id) AS ToTable,
       fk.delete_referential_action_desc    AS OnDelete
FROM sys.foreign_keys fk
ORDER BY FromTable, fk.name;
GO

PRINT N'--- 14. ІНДЕКСИ: перелік із ключовими стовпцями та фільтрами ---';
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name AS IndexName, i.type_desc AS IndexType,
       i.is_unique AS IsUnique, i.filter_definition AS Filter
FROM sys.indexes i
WHERE OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1 AND i.name IS NOT NULL
ORDER BY TableName, IndexName;
GO

PRINT N'--- 15. КОНТРОЛЬ: після всіх сценаріїв дані не змінилися ---';
SELECT (SELECT COUNT(*) FROM dbo.RepairOrders)             AS RepairOrders,
       (SELECT COUNT(*) FROM dbo.RepairOrderItems)         AS RepairOrderItems,
       (SELECT COUNT(*) FROM dbo.RepairOrderStatusHistory) AS StatusHistoryRows,
       (SELECT Status FROM dbo.RepairOrders WHERE Id = 2)  AS Order2Status,
       (SELECT Stock  FROM dbo.CatalogItems WHERE Id = 4)  AS Item4Stock;
GO

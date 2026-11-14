/* Проєкт №1 - RepairsDb. 6 збережуваних процедур; запускати після schema.sql.
   Процедури, що змінюють дані, працюють у явній транзакції з XACT_ABORT ON.
   Коди керованих помилок 50001–50010 описано в README. */

USE RepairsDb;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Приймання пристрою: замовлення, деталі та перший запис історії в одній транзакції
CREATE OR ALTER PROCEDURE dbo.usp_CreateRepairOrder
    @CustomerId         BIGINT,
    @DeviceType         NVARCHAR(50),
    @DeviceBrand        NVARCHAR(100)  = NULL,
    @DeviceModel        NVARCHAR(100),
    @SerialNumber       NVARCHAR(100)  = NULL,
    @DefectDescription  NVARCHAR(1000),
    @CreatedBy          NVARCHAR(100)  = NULL,
    @RepairOrderId      BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = @CustomerId AND IsDeleted = 0)
            THROW 50004, 'Customer not found', 1;

        INSERT INTO dbo.RepairOrders (CustomerId, CreatedBy)
        VALUES (@CustomerId, @CreatedBy);

        SET @RepairOrderId = SCOPE_IDENTITY();

        INSERT INTO dbo.RepairOrderDetails
               (RepairOrderId, DeviceType, DeviceBrand, DeviceModel, SerialNumber, DefectDescription)
        VALUES (@RepairOrderId, @DeviceType, @DeviceBrand, @DeviceModel, @SerialNumber, @DefectDescription);

        INSERT INTO dbo.RepairOrderStatusHistory (RepairOrderId, FromStatus, ToStatus, Comment, ChangedBy)
        VALUES (@RepairOrderId, NULL, N'Accepted', N'Пристрій прийнято в ремонт', @CreatedBy);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 2. Додати позицію: назва й ціна копіюються як знімок, для запчастин списується залишок
CREATE OR ALTER PROCEDURE dbo.usp_AddRepairOrderItem
    @RepairOrderId  BIGINT,
    @CatalogItemId  BIGINT,
    @Quantity       INT,
    @Discount       DECIMAL(18,2) = 0,
    @AddedBy        NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @Quantity IS NULL OR @Quantity <= 0
            THROW 50010, 'Quantity must be greater than zero', 1;

        BEGIN TRANSACTION;

        DECLARE @Status NVARCHAR(30) =
            (SELECT Status FROM dbo.RepairOrders WITH (UPDLOCK, ROWLOCK)
             WHERE Id = @RepairOrderId AND IsDeleted = 0);

        IF @Status IS NULL
            THROW 50001, 'Repair order not found', 1;
        IF @Status NOT IN (N'Accepted', N'Diagnosed', N'InRepair')
            THROW 50005, 'Items can be changed only before approval or during repair', 1;

        DECLARE @Name NVARCHAR(200), @Price DECIMAL(18,2), @ItemType NVARCHAR(20), @Stock INT;

        SELECT @Name = Name, @Price = Price, @ItemType = ItemType, @Stock = Stock
        FROM dbo.CatalogItems WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @CatalogItemId AND IsActive = 1;

        IF @Name IS NULL
            THROW 50006, 'Catalog item not found or inactive', 1;
        IF @ItemType = N'Part' AND @Stock < @Quantity
            THROW 50007, 'Insufficient stock for the requested part', 1;

        IF EXISTS (SELECT 1 FROM dbo.RepairOrderItems
                   WHERE RepairOrderId = @RepairOrderId AND CatalogItemId = @CatalogItemId)
            UPDATE dbo.RepairOrderItems
            SET Quantity = Quantity + @Quantity,
                Discount = Discount + @Discount
            WHERE RepairOrderId = @RepairOrderId AND CatalogItemId = @CatalogItemId;
        ELSE
            INSERT INTO dbo.RepairOrderItems
                   (RepairOrderId, CatalogItemId, ItemName, UnitPrice, Quantity, Discount, AddedBy)
            VALUES (@RepairOrderId, @CatalogItemId, @Name, @Price, @Quantity, @Discount, @AddedBy);

        IF @ItemType = N'Part'
            UPDATE dbo.CatalogItems
            SET Stock = Stock - @Quantity
            WHERE Id = @CatalogItemId;

        UPDATE dbo.RepairOrders
        SET UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @AddedBy
        WHERE Id = @RepairOrderId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* 3. Транзакційна зміна статусу. Допустимі переходи:
      Accepted -> Diagnosed -> Approved -> InRepair -> Ready -> Paid -> Issued;
      Cancelled — лише з Accepted, Diagnosed, Approved. Будь-який інший перехід — помилка 50002. */
CREATE OR ALTER PROCEDURE dbo.usp_ChangeRepairOrderStatus
    @RepairOrderId  BIGINT,
    @NewStatus      NVARCHAR(30),
    @ChangedBy      NVARCHAR(100) = NULL,
    @Comment        NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- UPDLOCK блокує рядок замовлення до кінця транзакції
        DECLARE @CurrentStatus NVARCHAR(30) =
            (SELECT Status FROM dbo.RepairOrders WITH (UPDLOCK, ROWLOCK)
             WHERE Id = @RepairOrderId AND IsDeleted = 0);

        IF @CurrentStatus IS NULL
            THROW 50001, 'Repair order not found', 1;

        -- матриця допустимих переходів
        IF NOT EXISTS (
            SELECT 1
            FROM (VALUES (N'Accepted',  N'Diagnosed'),
                         (N'Diagnosed', N'Approved'),
                         (N'Approved',  N'InRepair'),
                         (N'InRepair',  N'Ready'),
                         (N'Ready',     N'Paid'),
                         (N'Paid',      N'Issued'),
                         (N'Accepted',  N'Cancelled'),
                         (N'Diagnosed', N'Cancelled'),
                         (N'Approved',  N'Cancelled')) AS t (FromStatus, ToStatus)
            WHERE t.FromStatus = @CurrentStatus AND t.ToStatus = @NewStatus)
        BEGIN
            DECLARE @Message NVARCHAR(200) = CONCAT(
                N'Status transition is not allowed: ', @CurrentStatus, N' -> ', ISNULL(@NewStatus, N'NULL'));
            THROW 50002, @Message, 1;
        END

        IF @NewStatus = N'Approved'
           AND NOT EXISTS (SELECT 1 FROM dbo.RepairOrderItems WHERE RepairOrderId = @RepairOrderId)
            THROW 50003, 'Order has no items to approve', 1;

        -- скасування: зарезервовані запчастини повертаються на склад
        IF @NewStatus = N'Cancelled'
            UPDATE ci
            SET ci.Stock = ci.Stock + i.Quantity
            FROM dbo.CatalogItems ci
            JOIN dbo.RepairOrderItems i ON i.CatalogItemId = ci.Id
            WHERE i.RepairOrderId = @RepairOrderId AND ci.ItemType = N'Part';

        UPDATE dbo.RepairOrders
        SET Status    = @NewStatus,
            IssuedAt  = CASE WHEN @NewStatus = N'Issued' THEN SYSUTCDATETIME() ELSE IssuedAt END,
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @ChangedBy
        WHERE Id = @RepairOrderId;

        INSERT INTO dbo.RepairOrderStatusHistory (RepairOrderId, FromStatus, ToStatus, Comment, ChangedBy)
        VALUES (@RepairOrderId, @CurrentStatus, @NewStatus, @Comment, @ChangedBy);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 4. Замовлення з деталями, позиціями та історією (три набори результатів)
CREATE OR ALTER PROCEDURE dbo.usp_GetRepairOrderById
    @RepairOrderId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.RepairOrders WHERE Id = @RepairOrderId AND IsDeleted = 0)
        THROW 50001, 'Repair order not found', 1;

    SELECT  o.Id, o.Status, o.AcceptedAt, o.IssuedAt,
            o.CreatedAt, o.CreatedBy, o.UpdatedAt, o.UpdatedBy,
            o.CustomerId, c.FullName AS CustomerName, c.Email AS CustomerEmail, c.Phone AS CustomerPhone,
            d.DeviceType, d.DeviceBrand, d.DeviceModel, d.SerialNumber,
            d.DefectDescription, d.DiagnosticNotes,
            d.RowVersion AS DetailsRowVersion
    FROM dbo.RepairOrders o
    JOIN dbo.Customers c               ON c.Id = o.CustomerId
    LEFT JOIN dbo.RepairOrderDetails d ON d.RepairOrderId = o.Id
    WHERE o.Id = @RepairOrderId;

    SELECT  i.CatalogItemId, i.ItemName, i.UnitPrice, i.Quantity, i.Discount, i.LineTotal, i.AddedAt
    FROM dbo.RepairOrderItems i
    WHERE i.RepairOrderId = @RepairOrderId
    ORDER BY i.AddedAt, i.CatalogItemId;

    SELECT  h.FromStatus, h.ToStatus, h.Comment, h.ChangedAt, h.ChangedBy
    FROM dbo.RepairOrderStatusHistory h
    WHERE h.RepairOrderId = @RepairOrderId
    ORDER BY h.ChangedAt, h.Id;
END
GO

-- 5. Оновлення з оптимістичною конкурентністю: застарілий RowVersion — помилка 50008
CREATE OR ALTER PROCEDURE dbo.usp_UpdateRepairOrderDetails
    @RepairOrderId      BIGINT,
    @DefectDescription  NVARCHAR(1000),
    @DiagnosticNotes    NVARCHAR(2000) = NULL,
    @SerialNumber       NVARCHAR(100)  = NULL,
    @RowVersion         BINARY(8),
    @UpdatedBy          NVARCHAR(100)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE d
        SET d.DefectDescription = @DefectDescription,
            d.DiagnosticNotes   = @DiagnosticNotes,
            d.SerialNumber      = @SerialNumber,
            d.UpdatedAt         = SYSUTCDATETIME(),
            d.UpdatedBy         = @UpdatedBy
        FROM dbo.RepairOrderDetails d
        JOIN dbo.RepairOrders o ON o.Id = d.RepairOrderId
        WHERE d.RepairOrderId = @RepairOrderId
          AND o.IsDeleted = 0
          AND d.RowVersion = @RowVersion;        -- перевірка версії рядка

        IF @@ROWCOUNT = 0
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.RepairOrders WHERE Id = @RepairOrderId AND IsDeleted = 0)
                THROW 50001, 'Repair order not found', 1;
            THROW 50008, 'Concurrency conflict: the record was modified by another user', 1;
        END

        COMMIT TRANSACTION;

        SELECT RowVersion AS DetailsRowVersion
        FROM dbo.RepairOrderDetails
        WHERE RepairOrderId = @RepairOrderId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 6. М'яке видалення; дозволене лише для статусів Issued і Cancelled
CREATE OR ALTER PROCEDURE dbo.usp_SoftDeleteRepairOrder
    @RepairOrderId  BIGINT,
    @DeletedBy      NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Status NVARCHAR(30) =
            (SELECT Status FROM dbo.RepairOrders WITH (UPDLOCK, ROWLOCK)
             WHERE Id = @RepairOrderId AND IsDeleted = 0);

        IF @Status IS NULL
            THROW 50001, 'Repair order not found', 1;
        IF @Status NOT IN (N'Issued', N'Cancelled')
            THROW 50009, 'Only issued or cancelled orders can be deleted', 1;

        UPDATE dbo.RepairOrders
        SET IsDeleted = 1,
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @DeletedBy
        WHERE Id = @RepairOrderId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

PRINT N'RepairsDb: 6 stored procedures are up to date';
GO

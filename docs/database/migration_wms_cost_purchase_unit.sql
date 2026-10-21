/*
    One-time data migration for the WMS cost-unit convention.

    Before this change, CostPerUnit/UnitCost was interpreted as price per BaseUnit.
    The application now stores cost per PurchaseUnit and keeps quantities in BaseUnit.
    Run this script once against an existing database before deploying the new API.
    Do not run it again after the marker is created.
*/
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.WmsDataMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WmsDataMigrations
    (
        MigrationKey NVARCHAR(100) NOT NULL CONSTRAINT PK_WmsDataMigrations PRIMARY KEY,
        AppliedAt DATETIME2(0) NOT NULL CONSTRAINT DF_WmsDataMigrations_AppliedAt DEFAULT SYSDATETIME()
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.WmsDataMigrations
    WHERE MigrationKey = N'20261015-CostPerPurchaseUnit-v1'
)
BEGIN
    BEGIN TRANSACTION;

    UPDATE item
    SET CostPerUnit = CostPerUnit * CASE WHEN ConversionRate > 0 THEN ConversionRate ELSE 1 END,
        UpdatedAt = SYSDATETIME()
    FROM dbo.InventoryItems item;

    UPDATE batch
    SET UnitCost = batch.UnitCost * CASE WHEN item.ConversionRate > 0 THEN item.ConversionRate ELSE 1 END,
        UpdatedAt = SYSDATETIME()
    FROM dbo.InventoryBatches batch
    INNER JOIN dbo.InventoryItems item ON item.InventoryItemId = batch.InventoryItemId;

    UPDATE detail
    SET UnitCost = detail.UnitCost * CASE WHEN item.ConversionRate > 0 THEN item.ConversionRate ELSE 1 END,
        Subtotal = CASE
            WHEN item.ConversionRate > 0
                THEN (detail.Quantity / item.ConversionRate) * (detail.UnitCost * item.ConversionRate)
            ELSE detail.Quantity * detail.UnitCost
        END
    FROM dbo.StockReceiptDetails detail
    INNER JOIN dbo.InventoryItems item ON item.InventoryItemId = detail.InventoryItemId;

    UPDATE movement
    SET UnitCost = movement.UnitCost * CASE WHEN item.ConversionRate > 0 THEN item.ConversionRate ELSE 1 END
    FROM dbo.StockMovements movement
    INNER JOIN dbo.InventoryItems item ON item.InventoryItemId = movement.InventoryItemId
    WHERE movement.UnitCost IS NOT NULL;

    UPDATE receipt
    SET TotalAmount = ISNULL
    (
        (
            SELECT SUM(detail.Subtotal)
            FROM dbo.StockReceiptDetails detail
            WHERE detail.ReceiptId = receipt.ReceiptId
        ),
        0
    ),
    UpdatedAt = SYSDATETIME()
    FROM dbo.StockReceipts receipt;

    INSERT INTO dbo.WmsDataMigrations (MigrationKey)
    VALUES (N'20261015-CostPerPurchaseUnit-v1');

    COMMIT TRANSACTION;
END
GO

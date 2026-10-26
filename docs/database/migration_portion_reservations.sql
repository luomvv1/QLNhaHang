SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.OrderItems', 'PortionDate') IS NULL
    ALTER TABLE dbo.OrderItems ADD PortionDate date NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_PortionDate_Status' AND object_id = OBJECT_ID('dbo.OrderItems'))
    CREATE INDEX IX_OrderItems_PortionDate_Status ON dbo.OrderItems(PortionDate, ItemStatus)
        INCLUDE (FoodId, VariantId, Quantity, OrderId);
COMMIT TRANSACTION;

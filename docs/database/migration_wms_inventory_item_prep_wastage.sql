SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.InventoryItems', 'PrepWastagePercent') IS NULL
BEGIN
    ALTER TABLE dbo.InventoryItems ADD PrepWastagePercent DECIMAL(5, 2) NOT NULL
        CONSTRAINT DF_InventoryItems_PrepWastagePercent DEFAULT (0) WITH VALUES;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_InventoryItems_PrepWastagePercent')
BEGIN
    ALTER TABLE dbo.InventoryItems ADD CONSTRAINT CK_InventoryItems_PrepWastagePercent
        CHECK (PrepWastagePercent >= 0 AND PrepWastagePercent < 100);
END;
GO

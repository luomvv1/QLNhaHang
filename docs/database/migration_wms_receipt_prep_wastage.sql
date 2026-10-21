SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.StockReceiptDetails', 'GrossQuantity') IS NULL
    ALTER TABLE dbo.StockReceiptDetails ADD GrossQuantity DECIMAL(18, 4) NULL;
GO

IF COL_LENGTH('dbo.StockReceiptDetails', 'WastagePercent') IS NULL
BEGIN
    ALTER TABLE dbo.StockReceiptDetails ADD WastagePercent DECIMAL(5, 2) NOT NULL
        CONSTRAINT DF_StockReceiptDetails_WastagePercent DEFAULT (0) WITH VALUES;
END;
GO

IF COL_LENGTH('dbo.StockReceiptDetails', 'WastageQuantity') IS NULL
BEGIN
    ALTER TABLE dbo.StockReceiptDetails ADD WastageQuantity DECIMAL(18, 4) NOT NULL
        CONSTRAINT DF_StockReceiptDetails_WastageQuantity DEFAULT (0) WITH VALUES;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_StockReceiptDetails_Wastage')
    ALTER TABLE dbo.StockReceiptDetails ADD CONSTRAINT CK_StockReceiptDetails_Wastage
        CHECK (WastagePercent >= 0 AND WastagePercent < 100 AND WastageQuantity >= 0 AND (GrossQuantity IS NULL OR GrossQuantity >= Quantity));
GO

-- Existing receipt quantities are treated as usable quantities; no historical stock is recalculated.
UPDATE dbo.StockReceiptDetails
SET GrossQuantity = Quantity
WHERE GrossQuantity IS NULL;
GO

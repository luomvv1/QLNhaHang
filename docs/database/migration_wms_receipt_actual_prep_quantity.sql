SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.StockReceiptDetails', 'ExpectedUsableQuantity') IS NULL
    ALTER TABLE dbo.StockReceiptDetails ADD ExpectedUsableQuantity DECIMAL(18, 4) NULL;
GO

IF COL_LENGTH('dbo.StockReceiptDetails', 'AdjustmentNote') IS NULL
    ALTER TABLE dbo.StockReceiptDetails ADD AdjustmentNote NVARCHAR(500) NULL;
GO

-- Existing details keep their recorded stock quantity; no historical inventory is recalculated.
UPDATE dbo.StockReceiptDetails
SET ExpectedUsableQuantity = Quantity
WHERE ExpectedUsableQuantity IS NULL;
GO

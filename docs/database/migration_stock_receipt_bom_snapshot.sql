-- Capture the recipe context used to create kitchen issue receipts.
-- Run once after the existing WMS/BOM migrations.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.StockReceiptDetails', 'BomSnapshotJson') IS NULL
BEGIN
    ALTER TABLE dbo.StockReceiptDetails
        ADD BomSnapshotJson NVARCHAR(MAX) NULL;
END;
GO

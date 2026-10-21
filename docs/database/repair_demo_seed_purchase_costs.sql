SET XACT_ABORT ON;
GO

DECLARE @DemoCostFix TABLE
(
    BatchCode NVARCHAR(50) PRIMARY KEY,
    OldUnitCost DECIMAL(18, 2) NOT NULL,
    CorrectUnitCost DECIMAL(18, 2) NOT NULL
);

INSERT INTO @DemoCostFix (BatchCode, OldUnitCost, CorrectUnitCost) VALUES
('BAT-BOMY-FEFO1', 350, 350000),
('BAT-BOMY-FEFO2', 350, 350000),
('BAT-TOMSU-FEFO1', 280, 280000),
('BAT-TOMSU-FEFO2', 280, 280000),
('BAT-MUCONG-01', 240, 240000),
('BAT-MUCTRUNG-01', 220, 220000),
('BAT-BACHI-01', 160, 160000),
('BAT-COTDUA-01', 60, 24000),
('BAT-KHOAITAY-01', 45, 45000),
('BAT-GAO-01', 24, 600000),
('BAT-KEN-01', 18000, 432000),
('BAT-COCA-01', 10000, 240000),
('NCD-202610-A', 120, 120000),
('GV-202610-A', 80, 80000);

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE item
    SET item.CostPerUnit = fix.CorrectUnitCost,
        item.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.InventoryItems item
    JOIN dbo.InventoryBatches batch ON batch.InventoryItemId = item.InventoryItemId
    JOIN @DemoCostFix fix ON fix.BatchCode = batch.BatchCode
    WHERE item.CostPerUnit = fix.OldUnitCost;

    UPDATE batch
    SET batch.UnitCost = fix.CorrectUnitCost,
        batch.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.InventoryBatches batch
    JOIN @DemoCostFix fix ON fix.BatchCode = batch.BatchCode
    WHERE batch.UnitCost = fix.OldUnitCost;

    UPDATE movement
    SET movement.UnitCost = fix.CorrectUnitCost
    FROM dbo.StockMovements movement
    JOIN dbo.InventoryBatches batch ON batch.BatchId = movement.BatchId
    JOIN @DemoCostFix fix ON fix.BatchCode = batch.BatchCode
    WHERE movement.UnitCost = fix.OldUnitCost;

    UPDATE detail
    SET detail.UnitCost = fix.CorrectUnitCost,
        detail.Subtotal = ROUND(
            CASE WHEN item.ConversionRate > 0
                THEN (detail.Quantity / item.ConversionRate) * fix.CorrectUnitCost
                ELSE detail.Quantity * fix.CorrectUnitCost
            END, 2)
    FROM dbo.StockReceiptDetails detail
    JOIN dbo.InventoryItems item ON item.InventoryItemId = detail.InventoryItemId
    JOIN dbo.InventoryBatches batch ON batch.BatchId = detail.BatchId
    JOIN @DemoCostFix fix ON fix.BatchCode = batch.BatchCode
    WHERE detail.UnitCost = fix.OldUnitCost;

    UPDATE receipt
    SET receipt.TotalAmount = totals.TotalAmount,
        receipt.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.StockReceipts receipt
    JOIN
    (
        SELECT detail.ReceiptId, SUM(detail.Subtotal) AS TotalAmount
        FROM dbo.StockReceiptDetails detail
        WHERE EXISTS (SELECT 1 FROM @DemoCostFix fix WHERE fix.BatchCode IN
            (SELECT batch.BatchCode FROM dbo.InventoryBatches batch WHERE batch.BatchId = detail.BatchId))
        GROUP BY detail.ReceiptId
    ) totals ON totals.ReceiptId = receipt.ReceiptId;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT item.ItemName, item.PurchaseUnit, item.CostPerUnit,
       batch.BatchCode, batch.UnitCost AS BatchUnitCost,
       batch.QuantityRemaining, batch.ExpiryDate
FROM dbo.InventoryItems item
JOIN dbo.InventoryBatches batch ON batch.InventoryItemId = item.InventoryItemId
WHERE EXISTS (SELECT 1 FROM @DemoCostFix fix WHERE fix.BatchCode = batch.BatchCode)
ORDER BY item.ItemName, batch.ExpiryDate;
GO

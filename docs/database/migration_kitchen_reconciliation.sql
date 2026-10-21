SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.KitchenReconciliationLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.KitchenReconciliationLines (
        ReconciliationLineId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KitchenReconciliationLines PRIMARY KEY,
        SourceDetailId INT NOT NULL,
        IssuedQuantity DECIMAL(18,4) NOT NULL,
        UsedQuantity DECIMAL(18,4) NOT NULL,
        ReturnedQuantity DECIMAL(18,4) NOT NULL,
        WastedQuantity DECIMAL(18,4) NOT NULL,
        WasteReason NVARCHAR(500) NULL,
        ReturnReceiptId INT NULL,
        WasteReceiptId INT NULL,
        ReconciledBy INT NOT NULL,
        ReconciledAt DATETIME2(0) NOT NULL CONSTRAINT DF_KitchenReconciliationLines_ReconciledAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_KitchenReconciliationLines_SourceDetail UNIQUE (SourceDetailId),
        CONSTRAINT FK_KitchenReconciliationLines_Source FOREIGN KEY (SourceDetailId) REFERENCES dbo.StockReceiptDetails(DetailId),
        CONSTRAINT FK_KitchenReconciliationLines_ReturnReceipt FOREIGN KEY (ReturnReceiptId) REFERENCES dbo.StockReceipts(ReceiptId),
        CONSTRAINT FK_KitchenReconciliationLines_WasteReceipt FOREIGN KEY (WasteReceiptId) REFERENCES dbo.StockReceipts(ReceiptId),
        CONSTRAINT FK_KitchenReconciliationLines_User FOREIGN KEY (ReconciledBy) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_KitchenReconciliationLines_Quantity CHECK (
            IssuedQuantity > 0 AND UsedQuantity >= 0 AND ReturnedQuantity >= 0 AND WastedQuantity >= 0
            AND IssuedQuantity = UsedQuantity + ReturnedQuantity + WastedQuantity
            AND (WastedQuantity = 0 OR NULLIF(LTRIM(RTRIM(WasteReason)), '') IS NOT NULL)
        )
    );
    CREATE INDEX IX_KitchenReconciliationLines_ReconciledAt ON dbo.KitchenReconciliationLines(ReconciledAt);
END;
GO

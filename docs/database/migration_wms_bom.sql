-- ==============================================================
-- MIGRATION SCRIPT: MODULE KHO & ĐỊNH LƯỢNG SUẤT ĂN (WMS & BOM)
-- Phạm vi: 1 Cửa hàng / Căn tin độc lập - 1 Kho duy nhất
-- Ngày thực thi: 2026-10-15
-- ==============================================================

USE RestaurantManagement;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. NÂNG CẤP BẢNG InventoryItems (Đơn vị mua, đơn vị gốc, tỷ lệ quy đổi)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InventoryItems' AND COLUMN_NAME = 'PurchaseUnit')
BEGIN
    ALTER TABLE InventoryItems ADD PurchaseUnit NVARCHAR(50) NOT NULL CONSTRAINT DF_InventoryItems_PurchaseUnit DEFAULT N'kg';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InventoryItems' AND COLUMN_NAME = 'BaseUnit')
BEGIN
    ALTER TABLE InventoryItems ADD BaseUnit NVARCHAR(50) NOT NULL CONSTRAINT DF_InventoryItems_BaseUnit DEFAULT N'g';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InventoryItems' AND COLUMN_NAME = 'ConversionRate')
BEGIN
    ALTER TABLE InventoryItems ADD ConversionRate DECIMAL(18, 4) NOT NULL CONSTRAINT DF_InventoryItems_ConversionRate DEFAULT 1000;
END

-- Chuan hoa do chinh xac so luong theo BaseUnit cho schema cu.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryItems_LowStock' AND object_id = OBJECT_ID('dbo.InventoryItems'))
BEGIN
    DROP INDEX IX_InventoryItems_LowStock ON dbo.InventoryItems;
END

ALTER TABLE dbo.InventoryItems ALTER COLUMN CurrentQuantity DECIMAL(18, 4) NOT NULL;
ALTER TABLE dbo.InventoryItems ALTER COLUMN MinimumQuantity DECIMAL(18, 4) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryItems_LowStock' AND object_id = OBJECT_ID('dbo.InventoryItems'))
BEGIN
    CREATE INDEX IX_InventoryItems_LowStock ON dbo.InventoryItems (CurrentQuantity, MinimumQuantity);
END
GO

-- 2. NÂNG CẤP BẢNG FoodIngredients (hỗ trợ VariantId và hao hụt)
-- Chỉ thay bảng cũ một lần; các lần chạy sau không được làm mất khóa chính.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'FoodIngredients' AND COLUMN_NAME = 'FoodIngredientId')
BEGIN
    IF EXISTS (SELECT * FROM sys.key_constraints WHERE [name] = 'PK_FoodIngredients')
    BEGIN
        ALTER TABLE FoodIngredients DROP CONSTRAINT PK_FoodIngredients;
    END

    -- Tạo bảng tạm để di chuyển dữ liệu nếu cần
    CREATE TABLE FoodIngredients_New (
        FoodIngredientId INT IDENTITY(1,1) PRIMARY KEY,
        FoodId INT NOT NULL FOREIGN KEY REFERENCES Foods(FoodId),
        VariantId INT NULL FOREIGN KEY REFERENCES FoodVariants(VariantId),
        InventoryItemId INT NOT NULL FOREIGN KEY REFERENCES InventoryItems(InventoryItemId),
        QuantityPerUnit DECIMAL(18, 4) NOT NULL, -- Lượng tính theo BaseUnit
        WastagePercent DECIMAL(5, 2) NOT NULL DEFAULT 0, -- % hao hụt sơ chế (vd: 10% lọc mỡ/gân)
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    -- Sao chép dữ liệu cũ sang (nếu có)
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FoodIngredients')
    BEGIN
        INSERT INTO FoodIngredients_New (FoodId, VariantId, InventoryItemId, QuantityPerUnit, WastagePercent)
        SELECT FoodId, NULL, InventoryItemId, QuantityPerUnit, 0 FROM FoodIngredients;

        DROP TABLE FoodIngredients;
    END

    EXEC sp_rename 'FoodIngredients_New', 'FoodIngredients';
END
ELSE IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('dbo.FoodIngredients') AND type = 'PK')
BEGIN
    ALTER TABLE dbo.FoodIngredients ADD CONSTRAINT PK_FoodIngredients PRIMARY KEY (FoodIngredientId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_FoodIngredients_BOM' AND object_id = OBJECT_ID('dbo.FoodIngredients'))
BEGIN
    CREATE UNIQUE INDEX UQ_FoodIngredients_BOM
        ON dbo.FoodIngredients (FoodId, VariantId, InventoryItemId);
END
GO

-- 3. TẠO BẢNG InventoryBatches (Quản lý Lô hàng & Hạn sử dụng - FEFO / FIFO)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'InventoryBatches')
BEGIN
    CREATE TABLE InventoryBatches (
        BatchId INT IDENTITY(1,1) PRIMARY KEY,
        InventoryItemId INT NOT NULL FOREIGN KEY REFERENCES InventoryItems(InventoryItemId),
        BatchCode NVARCHAR(50) NOT NULL,
        QuantityRemaining DECIMAL(18, 4) NOT NULL, -- Số lượng còn lại tính theo BaseUnit
        OriginalQuantity DECIMAL(18, 4) NOT NULL,  -- Số lượng nhập ban đầu theo BaseUnit
        UnitCost DECIMAL(18, 2) NOT NULL,          -- Đơn giá vốn theo PurchaseUnit
        ManufacturingDate DATE NULL,
        ExpiryDate DATE NOT NULL,                  -- Hạn sử dụng (Trọng tâm cho FEFO)
        ReceivedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Ngày nhập (Trọng tâm cho FIFO)
        SupplierId INT NULL FOREIGN KEY REFERENCES Suppliers(SupplierId),
        Status NVARCHAR(20) NOT NULL DEFAULT 'active', -- active, depleted, expired
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_InventoryBatches_Item_Expiry ON InventoryBatches (InventoryItemId, ExpiryDate, ReceivedDate, Status);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_InventoryBatches_Item_Code' AND object_id = OBJECT_ID('dbo.InventoryBatches'))
BEGIN
    CREATE UNIQUE INDEX UQ_InventoryBatches_Item_Code
        ON dbo.InventoryBatches (InventoryItemId, BatchCode);
END
GO

-- 4. TẠO BẢNG StockReceipts (Phiếu nhập, xuất bếp, hoàn kho, xuất hủy, kiểm kê)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockReceipts')
BEGIN
    CREATE TABLE StockReceipts (
        ReceiptId INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptCode NVARCHAR(50) NOT NULL UNIQUE,
        ReceiptType NVARCHAR(50) NOT NULL, -- import_supplier, export_kitchen, return_from_kitchen, waste_disposal, audit_adjustment
        Status NVARCHAR(20) NOT NULL DEFAULT 'draft', -- draft, approved, cancelled
        SupplierId INT NULL FOREIGN KEY REFERENCES Suppliers(SupplierId),
        TotalAmount DECIMAL(18, 2) NOT NULL DEFAULT 0,
        Note NVARCHAR(500) NULL,
        CreatedBy INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        ApprovedBy INT NULL FOREIGN KEY REFERENCES Users(UserId),
        ApprovedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 5. TẠO BẢNG StockReceiptDetails (Chi tiết nguyên liệu trong từng phiếu kho)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockReceiptDetails')
BEGIN
    CREATE TABLE StockReceiptDetails (
        DetailId INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptId INT NOT NULL FOREIGN KEY REFERENCES StockReceipts(ReceiptId) ON DELETE CASCADE,
        InventoryItemId INT NOT NULL FOREIGN KEY REFERENCES InventoryItems(InventoryItemId),
        BatchId INT NULL FOREIGN KEY REFERENCES InventoryBatches(BatchId),
        Quantity DECIMAL(18, 4) NOT NULL, -- Số lượng theo BaseUnit
        UnitCost DECIMAL(18, 2) NOT NULL,          -- Đơn giá vốn theo PurchaseUnit
        Subtotal DECIMAL(18, 2) NOT NULL,
        Note NVARCHAR(255) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockReceiptDetails' AND COLUMN_NAME = 'NewBatchCode')
    ALTER TABLE dbo.StockReceiptDetails ADD NewBatchCode NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockReceiptDetails' AND COLUMN_NAME = 'ManufacturingDate')
    ALTER TABLE dbo.StockReceiptDetails ADD ManufacturingDate DATE NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockReceiptDetails' AND COLUMN_NAME = 'ExpiryDate')
    ALTER TABLE dbo.StockReceiptDetails ADD ExpiryDate DATE NULL;
GO

-- 6. TẠO BẢNG DailyPortionControls (Điều phối suất ăn hàng ngày - Realtime BOM bếp)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DailyPortionControls')
BEGIN
    CREATE TABLE DailyPortionControls (
        PortionControlId INT IDENTITY(1,1) PRIMARY KEY,
        ControlDate DATE NOT NULL,
        FoodId INT NOT NULL FOREIGN KEY REFERENCES Foods(FoodId),
        VariantId INT NULL FOREIGN KEY REFERENCES FoodVariants(VariantId),
        PlannedPortions DECIMAL(18, 2) NOT NULL DEFAULT 0,    -- Suất dự kiến đầu ngày
        CarryOverPortions DECIMAL(18, 2) NOT NULL DEFAULT 0,  -- Tồn bán thành phẩm hôm qua chuyển sang
        CookedPortions DECIMAL(18, 2) NOT NULL DEFAULT 0,     -- Thực tế bếp nấu ra
        SoldPortions DECIMAL(18, 2) NOT NULL DEFAULT 0,       -- Đã bán/phục vụ qua POS
        ActualRemain DECIMAL(18, 2) NULL,                     -- Đếm thực tế cuối ngày
        DisposedPortions DECIMAL(18, 2) NOT NULL DEFAULT 0,   -- Đổ bỏ do quá hạn/hỏng
        NextCarryOverPortions DECIMAL(18, 2) NOT NULL DEFAULT 0, -- Lưu tủ mát bán tiếp ngày mai
        Status NVARCHAR(20) NOT NULL DEFAULT 'planned',       -- planned, in_service, reconciled, closed
        Note NVARCHAR(255) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX UQ_DailyPortionControls_Date_Item ON DailyPortionControls (ControlDate, FoodId, VariantId);
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DailyPortionControls' AND COLUMN_NAME = 'SystemRemain')
BEGIN
    ALTER TABLE dbo.DailyPortionControls
        ADD SystemRemain AS (CookedPortions - SoldPortions) PERSISTED;
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DailyPortionControls' AND COLUMN_NAME = 'VariancePortions')
BEGIN
    ALTER TABLE dbo.DailyPortionControls
        ADD VariancePortions AS (CASE WHEN ActualRemain IS NULL THEN NULL ELSE ActualRemain - (CookedPortions - SoldPortions) END) PERSISTED;
END
GO

-- 7. TẠO BẢNG StockAudits & StockAuditItems (Kiểm kê định kỳ kho)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockAudits')
BEGIN
    CREATE TABLE StockAudits (
        AuditId INT IDENTITY(1,1) PRIMARY KEY,
        AuditCode NVARCHAR(50) NOT NULL UNIQUE,
        AuditDate DATE NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'draft', -- draft, completed, cancelled
        Note NVARCHAR(500) NULL,
        CreatedBy INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        CompletedBy INT NULL FOREIGN KEY REFERENCES Users(UserId),
        CompletedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockAuditItems')
BEGIN
    CREATE TABLE StockAuditItems (
        AuditItemId INT IDENTITY(1,1) PRIMARY KEY,
        AuditId INT NOT NULL FOREIGN KEY REFERENCES StockAudits(AuditId) ON DELETE CASCADE,
        InventoryItemId INT NOT NULL FOREIGN KEY REFERENCES InventoryItems(InventoryItemId),
        SystemQuantity DECIMAL(18, 4) NOT NULL,
        ActualQuantity DECIMAL(18, 4) NOT NULL,
        DifferenceQuantity DECIMAL(18, 4) NOT NULL,
        Reason NVARCHAR(255) NULL
    );
END
GO

-- 8. BỔ SUNG CỘT BatchId VÀO BẢNG StockMovements ĐỂ LIÊN KẾT LÔ HÀNG
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockMovements' AND COLUMN_NAME = 'BatchId')
BEGIN
    ALTER TABLE StockMovements ADD BatchId INT NULL FOREIGN KEY REFERENCES InventoryBatches(BatchId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_Batch_Date' AND object_id = OBJECT_ID('dbo.StockMovements'))
BEGIN
    CREATE INDEX IX_StockMovements_Batch_Date
        ON dbo.StockMovements (BatchId, CreatedAt DESC);
END

ALTER TABLE dbo.StockMovements ALTER COLUMN Quantity DECIMAL(18, 4) NOT NULL;
GO

-- 9. BỔ SUNG CÁC RÀNG BUỘC NGHIỆP VỤ
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_InventoryItems_ConversionRate')
    ALTER TABLE dbo.InventoryItems ADD CONSTRAINT CK_InventoryItems_ConversionRate CHECK (ConversionRate > 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_InventoryBatches_Quantity')
    ALTER TABLE dbo.InventoryBatches ADD CONSTRAINT CK_InventoryBatches_Quantity CHECK (OriginalQuantity > 0 AND QuantityRemaining >= 0 AND QuantityRemaining <= OriginalQuantity);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_InventoryBatches_UnitCost')
    ALTER TABLE dbo.InventoryBatches ADD CONSTRAINT CK_InventoryBatches_UnitCost CHECK (UnitCost >= 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_FoodIngredients_Quantity')
    ALTER TABLE dbo.FoodIngredients ADD CONSTRAINT CK_FoodIngredients_Quantity CHECK (QuantityPerUnit > 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_FoodIngredients_Wastage')
    ALTER TABLE dbo.FoodIngredients ADD CONSTRAINT CK_FoodIngredients_Wastage CHECK (WastagePercent >= 0 AND WastagePercent <= 100);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_StockReceiptDetails_Quantity')
    ALTER TABLE dbo.StockReceiptDetails ADD CONSTRAINT CK_StockReceiptDetails_Quantity CHECK (Quantity > 0 AND UnitCost >= 0 AND Subtotal >= 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_StockReceipts_Type')
    ALTER TABLE dbo.StockReceipts ADD CONSTRAINT CK_StockReceipts_Type CHECK (ReceiptType IN ('import_supplier','export_kitchen','return_from_kitchen','waste_disposal','audit_adjustment'));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_StockReceipts_Status')
    ALTER TABLE dbo.StockReceipts ADD CONSTRAINT CK_StockReceipts_Status CHECK (Status IN ('draft','approved','cancelled'));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DailyPortionControls_NonNegative')
    ALTER TABLE dbo.DailyPortionControls ADD CONSTRAINT CK_DailyPortionControls_NonNegative CHECK (PlannedPortions >= 0 AND CarryOverPortions >= 0 AND CookedPortions >= 0 AND SoldPortions >= 0 AND DisposedPortions >= 0 AND NextCarryOverPortions >= 0 AND (ActualRemain IS NULL OR ActualRemain >= 0));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_StockAuditItems_Quantity')
    ALTER TABLE dbo.StockAuditItems ADD CONSTRAINT CK_StockAuditItems_Quantity CHECK (SystemQuantity >= 0 AND ActualQuantity >= 0);
GO

COMMIT TRANSACTION;
PRINT N'==> MIGRATION THÀNH CÔNG: ĐÃ CẬP NHẬT CSDL MODULE WMS & BOM 1 KHO!';

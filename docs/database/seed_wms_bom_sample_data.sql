-- ============================================================================
-- SEED DATA: WMS/BOM cho 1 nha hang - 10 mon mau
-- Chay sau restaurant_schema.sql va migration_wms_bom.sql.
-- Script an toan khi chay lai: khong phu thuoc ID tu tang va khong DELETE du lieu.
-- Chi dung cho moi truong dev/test, khong dung lam du lieu kho dau ky production.
-- ============================================================================

USE RestaurantManagement;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2(0) = SYSDATETIME();
DECLARE @Today DATE = CAST(@Now AS DATE);
DECLARE @AdminUserId INT;

SELECT TOP (1) @AdminUserId = u.UserId
FROM dbo.Users u
JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleCode IN ('admin', 'manager')
ORDER BY CASE WHEN r.RoleCode = 'admin' THEN 0 ELSE 1 END, u.UserId;

IF @AdminUserId IS NULL
    SELECT TOP (1) @AdminUserId = UserId FROM dbo.Users WHERE IsActive = 1 ORDER BY UserId;

IF @AdminUserId IS NULL
    THROW 51000, 'Can mot user dang hoat dong de tao du lieu mau WMS/BOM.', 1;

BEGIN TRANSACTION;

-- 1. Nha cung cap mau, tim theo ten thay vi hard-code SupplierId.
DECLARE @SupplierSeed TABLE
(
    SupplierName NVARCHAR(200) NOT NULL,
    Phone VARCHAR(20) NULL,
    Email VARCHAR(255) NULL,
    Address NVARCHAR(500) NULL
);

INSERT INTO @SupplierSeed VALUES
(N'Hai san Tan Cang', '0901000001', 'haisan@example.test', N'Quan 4, TP. Ho Chi Minh'),
(N'Thuc pham tuoi An Tam', '0901000002', 'thucpham@example.test', N'Quan 10, TP. Ho Chi Minh'),
(N'Tap hoa Nha Hang', '0901000003', 'taphoa@example.test', N'Quan 1, TP. Ho Chi Minh');

INSERT INTO dbo.Suppliers (SupplierName, Phone, Email, Address, IsActive, CreatedAt)
SELECT s.SupplierName, s.Phone, s.Email, s.Address, 1, @Now
FROM @SupplierSeed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.Suppliers x WHERE x.SupplierName = s.SupplierName);

-- 2. Danh muc va mon mau.
DECLARE @CategorySeed TABLE
(
    CategoryName NVARCHAR(100) NOT NULL,
    SortOrder INT NOT NULL
);

INSERT INTO @CategorySeed VALUES
(N'Lau', 1), (N'Nuong', 2), (N'Hai san', 3), (N'Mon phu', 4);

INSERT INTO dbo.FoodCategories (CategoryName, SortOrder, IsActive, CreatedAt, UpdatedAt)
SELECT c.CategoryName, c.SortOrder, 1, @Now, @Now
FROM @CategorySeed c
WHERE NOT EXISTS (SELECT 1 FROM dbo.FoodCategories x WHERE x.CategoryName = c.CategoryName);

DECLARE @FoodSeed TABLE
(
    FoodName NVARCHAR(200) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(1000) NULL,
    MinPrice DECIMAL(18,2) NOT NULL
);

INSERT INTO @FoodSeed VALUES
(N'Ga Ta Hap La Chanh', N'Nuong', N'Ga ta hap la chanh, phuc vu theo phan.', 189000),
(N'Ga Nuong Muoi Ot', N'Nuong', N'Ga nuong muoi ot.', 219000),
(N'Lau Thai Chua Cay Hai San', N'Lau', N'Lau Thai hai san chua cay.', 349000),
(N'Lau Nam Ga Ta Thao Moc', N'Lau', N'Lau nam ga ta thao moc.', 420000),
(N'De Suon Bo My Nuong Sot Tieu', N'Nuong', N'De suon bo My nuong sot tieu.', 399000),
(N'Ba Chi Heo Nuong Sot Kim Chi', N'Nuong', N'Ba chi heo nuong sot kim chi.', 239000),
(N'Muc Trung Hap Gung Hanh', N'Hai san', N'Muc trung hap gung hanh.', 229000),
(N'Tom Su Nuong Muoi Ot', N'Hai san', N'Tom su nuong muoi ot.', 259000),
(N'Khoai Tay Chien Bo Toi', N'Mon phu', N'Khoai tay chien bo toi.', 79000),
(N'Com Chien Hai San Hoang Kim', N'Mon phu', N'Com chien hai san hoang kim.', 129000);

INSERT INTO dbo.Foods
    (CategoryId, FoodName, Description, FoodStatus, MinPrice, IsActive, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
SELECT c.CategoryId, f.FoodName, f.Description, 'available', f.MinPrice, 1, @AdminUserId, @AdminUserId, @Now, @Now
FROM @FoodSeed f
JOIN dbo.FoodCategories c ON c.CategoryName = f.CategoryName
WHERE NOT EXISTS (SELECT 1 FROM dbo.Foods x WHERE x.FoodName = f.FoodName);

UPDATE food
SET food.MinPrice = seed.MinPrice,
    food.Description = seed.Description,
    food.IsActive = 1,
    food.FoodStatus = 'available',
    food.UpdatedBy = @AdminUserId,
    food.UpdatedAt = @Now
FROM dbo.Foods food
JOIN @FoodSeed seed ON seed.FoodName = food.FoodName;

DECLARE @VariantSeed TABLE
(
    FoodName NVARCHAR(200) NOT NULL,
    VariantName NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Unit NVARCHAR(30) NOT NULL,
    SortOrder INT NOT NULL
);

INSERT INTO @VariantSeed VALUES
(N'Ga Ta Hap La Chanh', N'1 phan', 189000, N'phan', 1),
(N'Ga Ta Hap La Chanh', N'2-3 nguoi', 349000, N'phan', 2),
(N'Ga Nuong Muoi Ot', N'1 phan', 219000, N'phan', 1),
(N'Ga Nuong Muoi Ot', N'2-3 nguoi', 399000, N'phan', 2),
(N'Lau Thai Chua Cay Hai San', N'Nho', 349000, N'noi', 1),
(N'Lau Thai Chua Cay Hai San', N'Lon', 499000, N'noi', 2),
(N'Lau Nam Ga Ta Thao Moc', N'Nho', 420000, N'noi', 1),
(N'Lau Nam Ga Ta Thao Moc', N'Lon', 590000, N'noi', 2),
(N'De Suon Bo My Nuong Sot Tieu', N'1 phan', 399000, N'phan', 1),
(N'De Suon Bo My Nuong Sot Tieu', N'2-3 nguoi', 699000, N'phan', 2),
(N'Ba Chi Heo Nuong Sot Kim Chi', N'1 phan', 239000, N'phan', 1),
(N'Ba Chi Heo Nuong Sot Kim Chi', N'2-3 nguoi', 429000, N'phan', 2),
(N'Muc Trung Hap Gung Hanh', N'1 phan', 229000, N'phan', 1),
(N'Muc Trung Hap Gung Hanh', N'2-3 nguoi', 399000, N'phan', 2),
(N'Tom Su Nuong Muoi Ot', N'1 phan', 259000, N'phan', 1),
(N'Tom Su Nuong Muoi Ot', N'2-3 nguoi', 459000, N'phan', 2),
(N'Khoai Tay Chien Bo Toi', N'1 phan', 79000, N'dia', 1),
(N'Khoai Tay Chien Bo Toi', N'2 phan', 139000, N'dia', 2),
(N'Com Chien Hai San Hoang Kim', N'1 phan', 129000, N'dia', 1),
(N'Com Chien Hai San Hoang Kim', N'2 phan', 229000, N'dia', 2);

INSERT INTO dbo.FoodVariants (FoodId, VariantName, Price, Unit, SortOrder, IsActive, CreatedAt, UpdatedAt)
SELECT f.FoodId, v.VariantName, v.Price, v.Unit, v.SortOrder, 1, @Now, @Now
FROM @VariantSeed v
JOIN dbo.Foods f ON f.FoodName = v.FoodName
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.FoodVariants x
    WHERE x.FoodId = f.FoodId AND x.VariantName = v.VariantName
);

-- 3. Nguyen lieu. CurrentQuantity luu theo BaseUnit; CostPerUnit la gia/PurchaseUnit.
DECLARE @ItemSeed TABLE
(
    ItemName NVARCHAR(200) NOT NULL,
    SupplierName NVARCHAR(200) NOT NULL,
    PurchaseUnit NVARCHAR(50) NOT NULL,
    BaseUnit NVARCHAR(50) NOT NULL,
    ConversionRate DECIMAL(18,4) NOT NULL,
    MinimumQuantity DECIMAL(18,4) NOT NULL,
    CostPerUnit DECIMAL(18,2) NOT NULL
);

INSERT INTO @ItemSeed VALUES
(N'Tom su', N'Hai san Tan Cang', N'kg', N'g', 1000, 5000, 260000),
(N'Muc ong', N'Hai san Tan Cang', N'kg', N'g', 1000, 5000, 220000),
(N'Muc trung', N'Hai san Tan Cang', N'kg', N'g', 1000, 3000, 210000),
(N'Thit bo My', N'Thuc pham tuoi An Tam', N'kg', N'g', 1000, 5000, 280000),
(N'De suon bo My', N'Thuc pham tuoi An Tam', N'kg', N'g', 1000, 5000, 310000),
(N'Ba chi heo', N'Thuc pham tuoi An Tam', N'kg', N'g', 1000, 5000, 170000),
(N'Ga ta', N'Thuc pham tuoi An Tam', N'con', N'g', 1200, 6000, 138000),
(N'Nam kim cham', N'Thuc pham tuoi An Tam', N'goi', N'g', 200, 2000, 19000),
(N'Gao te', N'Tap hoa Nha Hang', N'kg', N'g', 1000, 10000, 28000),
(N'Khoai tay', N'Tap hoa Nha Hang', N'kg', N'g', 1000, 5000, 32000),
(N'Nuoc cot dua', N'Tap hoa Nha Hang', N'lit', N'ml', 1000, 3000, 120000),
(N'Gia vi tong hop', N'Tap hoa Nha Hang', N'kg', N'g', 1000, 1000, 80000);

INSERT INTO dbo.InventoryItems
    (SupplierId, ItemName, Unit, PurchaseUnit, BaseUnit, ConversionRate, CurrentQuantity, MinimumQuantity, CostPerUnit, IsActive, CreatedAt, UpdatedAt)
SELECT s.SupplierId, i.ItemName, i.BaseUnit, i.PurchaseUnit, i.BaseUnit, i.ConversionRate, 0, i.MinimumQuantity, i.CostPerUnit, 1, @Now, @Now
FROM @ItemSeed i
JOIN dbo.Suppliers s ON s.SupplierName = i.SupplierName
WHERE NOT EXISTS (SELECT 1 FROM dbo.InventoryItems x WHERE x.ItemName = i.ItemName);

UPDATE item
SET item.SupplierId = s.SupplierId,
    item.Unit = seed.BaseUnit,
    item.PurchaseUnit = seed.PurchaseUnit,
    item.BaseUnit = seed.BaseUnit,
    item.ConversionRate = seed.ConversionRate,
    item.MinimumQuantity = seed.MinimumQuantity,
    item.CostPerUnit = seed.CostPerUnit,
    item.IsActive = 1,
    item.UpdatedAt = @Now
FROM dbo.InventoryItems item
JOIN @ItemSeed seed ON seed.ItemName = item.ItemName
JOIN dbo.Suppliers s ON s.SupplierName = seed.SupplierName;

-- 4. Nhap kho dau ky bang phieu + lo + movement. Khong update CurrentQuantity neu khong co movement.
DECLARE @ReceiptCode NVARCHAR(50) = N'PNK-DEMO-' + CONVERT(CHAR(8), @Today, 112);
DECLARE @ReceiptId INT;

SELECT @ReceiptId = ReceiptId FROM dbo.StockReceipts WHERE ReceiptCode = @ReceiptCode;

IF @ReceiptId IS NULL
BEGIN
    INSERT INTO dbo.StockReceipts
        (ReceiptCode, ReceiptType, Status, SupplierId, TotalAmount, Note, CreatedBy, ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt)
    VALUES
        (@ReceiptCode, 'import_supplier', 'approved', NULL, 0, N'Du lieu mau dau ky WMS/BOM', @AdminUserId, @AdminUserId, @Now, @Now, @Now);
    SET @ReceiptId = SCOPE_IDENTITY();
END

DECLARE @BatchSeed TABLE
(
    ItemName NVARCHAR(200) NOT NULL,
    BatchCode NVARCHAR(50) NOT NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    UnitCost DECIMAL(18,2) NOT NULL,
    ExpiryDays INT NOT NULL,
    ReceivedDaysAgo INT NOT NULL
);

INSERT INTO @BatchSeed VALUES
(N'Tom su', N'TS-202610-A', 12000, 260000, 3, 1),
(N'Tom su', N'TS-202610-B', 10000, 255000, 5, 0),
(N'Muc ong', N'MO-202610-A', 10000, 220000, 4, 1),
(N'Muc trung', N'MT-202610-A', 8000, 210000, 3, 0),
(N'Thit bo My', N'BO-202610-A', 15000, 280000, 10, 2),
(N'De suon bo My', N'DS-202610-A', 12000, 310000, 12, 1),
(N'Ba chi heo', N'BC-202610-A', 14000, 170000, 8, 1),
(N'Ga ta', N'GA-202610-A', 30000, 138000, 5, 0),
(N'Ga ta', N'GA-202610-B', 20000, 141600, 7, 0),
(N'Nam kim cham', N'NK-202610-A', 8000, 19000, 4, 0),
(N'Gao te', N'GAO-202610-A', 50000, 28000, 180, 7),
(N'Khoai tay', N'KT-202610-A', 20000, 32000, 30, 3),
(N'Nuoc cot dua', N'NCD-202610-A', 10000, 120000, 90, 5),
(N'Gia vi tong hop', N'GV-202610-A', 5000, 80000, 365, 10);

DECLARE @ItemId INT, @BatchId INT, @Quantity DECIMAL(18,4), @UnitCost DECIMAL(18,2), @ExpiryDays INT, @ReceivedDaysAgo INT;
DECLARE @ItemName NVARCHAR(200), @BatchCode NVARCHAR(50);

DECLARE batch_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT ItemName, BatchCode, Quantity, UnitCost, ExpiryDays, ReceivedDaysAgo FROM @BatchSeed;
OPEN batch_cursor;
FETCH NEXT FROM batch_cursor INTO @ItemName, @BatchCode, @Quantity, @UnitCost, @ExpiryDays, @ReceivedDaysAgo;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @ItemId = NULL;
    SET @BatchId = NULL;
    SELECT @ItemId = InventoryItemId FROM dbo.InventoryItems WHERE ItemName = @ItemName;
    SELECT @BatchId = BatchId FROM dbo.InventoryBatches WHERE InventoryItemId = @ItemId AND BatchCode = @BatchCode;

    IF @BatchId IS NULL
    BEGIN
        INSERT INTO dbo.InventoryBatches
            (InventoryItemId, BatchCode, QuantityRemaining, OriginalQuantity, UnitCost, ManufacturingDate, ExpiryDate, ReceivedDate, SupplierId, Status, CreatedAt, UpdatedAt)
        SELECT @ItemId, @BatchCode, @Quantity, @Quantity, @UnitCost, DATEADD(DAY, -@ReceivedDaysAgo - 2, @Today), DATEADD(DAY, @ExpiryDays, @Today), DATEADD(DAY, -@ReceivedDaysAgo, @Now), SupplierId, 'active', @Now, @Now
        FROM dbo.InventoryItems WHERE InventoryItemId = @ItemId;
        SET @BatchId = SCOPE_IDENTITY();

        INSERT INTO dbo.StockReceiptDetails (ReceiptId, InventoryItemId, BatchId, Quantity, UnitCost, Subtotal, Note)
        SELECT @ReceiptId, @ItemId, @BatchId, @Quantity, @UnitCost,
               CASE WHEN ConversionRate > 0 THEN (@Quantity / ConversionRate) * @UnitCost ELSE @Quantity * @UnitCost END,
               N'Lo mau dau ky'
        FROM dbo.InventoryItems
        WHERE InventoryItemId = @ItemId;

        INSERT INTO dbo.StockMovements
            (InventoryItemId, BatchId, MovementType, Quantity, UnitCost, Reason, ReferenceType, ReferenceId, CreatedBy, CreatedAt)
        VALUES
            (@ItemId, @BatchId, 'import', @Quantity, @UnitCost, N'Nhap kho dau ky du lieu mau', 'stock_receipt', @ReceiptId, @AdminUserId, @Now);

        UPDATE dbo.InventoryItems
        SET CurrentQuantity = CurrentQuantity + @Quantity, UpdatedAt = @Now
        WHERE InventoryItemId = @ItemId;
    END

    FETCH NEXT FROM batch_cursor INTO @ItemName, @BatchCode, @Quantity, @UnitCost, @ExpiryDays, @ReceivedDaysAgo;
END

CLOSE batch_cursor;
DEALLOCATE batch_cursor;

UPDATE dbo.StockReceipts
SET TotalAmount = ISNULL((SELECT SUM(Subtotal) FROM dbo.StockReceiptDetails WHERE ReceiptId = @ReceiptId), 0), UpdatedAt = @Now
WHERE ReceiptId = @ReceiptId;

-- 5. BOM. QuantityPerUnit la luong xuat kho theo BaseUnit, da bao gom luong gross can dung.
-- WastagePercent chi mo ta hao hut so che; backend khong duoc nhan hao hut lan hai.
DECLARE @BomSeed TABLE
(
    FoodName NVARCHAR(200) NOT NULL,
    VariantName NVARCHAR(100) NOT NULL,
    ItemName NVARCHAR(200) NOT NULL,
    QuantityPerUnit DECIMAL(18,4) NOT NULL,
    WastagePercent DECIMAL(5,2) NOT NULL
);

INSERT INTO @BomSeed VALUES
(N'Ga Ta Hap La Chanh', N'1 phan', N'Ga ta', 600, 8),
(N'Ga Ta Hap La Chanh', N'2-3 nguoi', N'Ga ta', 1100, 8),
(N'Ga Nuong Muoi Ot', N'1 phan', N'Ga ta', 650, 10),
(N'Ga Nuong Muoi Ot', N'2-3 nguoi', N'Ga ta', 1200, 10),
(N'Lau Thai Chua Cay Hai San', N'Nho', N'Tom su', 180, 5),
(N'Lau Thai Chua Cay Hai San', N'Nho', N'Muc ong', 180, 5),
(N'Lau Thai Chua Cay Hai San', N'Lon', N'Tom su', 300, 5),
(N'Lau Thai Chua Cay Hai San', N'Lon', N'Muc ong', 300, 5),
(N'Lau Nam Ga Ta Thao Moc', N'Nho', N'Ga ta', 500, 8),
(N'Lau Nam Ga Ta Thao Moc', N'Nho', N'Nam kim cham', 200, 3),
(N'Lau Nam Ga Ta Thao Moc', N'Lon', N'Ga ta', 900, 8),
(N'Lau Nam Ga Ta Thao Moc', N'Lon', N'Nam kim cham', 350, 3),
(N'De Suon Bo My Nuong Sot Tieu', N'1 phan', N'De suon bo My', 450, 6),
(N'De Suon Bo My Nuong Sot Tieu', N'2-3 nguoi', N'De suon bo My', 800, 6),
(N'Ba Chi Heo Nuong Sot Kim Chi', N'1 phan', N'Ba chi heo', 350, 5),
(N'Ba Chi Heo Nuong Sot Kim Chi', N'2-3 nguoi', N'Ba chi heo', 650, 5),
(N'Muc Trung Hap Gung Hanh', N'1 phan', N'Muc trung', 300, 5),
(N'Muc Trung Hap Gung Hanh', N'2-3 nguoi', N'Muc trung', 550, 5),
(N'Tom Su Nuong Muoi Ot', N'1 phan', N'Tom su', 300, 5),
(N'Tom Su Nuong Muoi Ot', N'2-3 nguoi', N'Tom su', 550, 5),
(N'Khoai Tay Chien Bo Toi', N'1 phan', N'Khoai tay', 220, 3),
(N'Khoai Tay Chien Bo Toi', N'2 phan', N'Khoai tay', 400, 3),
(N'Com Chien Hai San Hoang Kim', N'1 phan', N'Gao te', 180, 2),
(N'Com Chien Hai San Hoang Kim', N'1 phan', N'Tom su', 50, 5),
(N'Com Chien Hai San Hoang Kim', N'2 phan', N'Gao te', 330, 2),
(N'Com Chien Hai San Hoang Kim', N'2 phan', N'Tom su', 90, 5);

UPDATE bom
SET bom.QuantityPerUnit = seed.QuantityPerUnit,
    bom.WastagePercent = seed.WastagePercent
FROM dbo.FoodIngredients bom
JOIN dbo.Foods f ON f.FoodId = bom.FoodId
JOIN dbo.FoodVariants v ON v.VariantId = bom.VariantId
JOIN dbo.InventoryItems i ON i.InventoryItemId = bom.InventoryItemId
JOIN @BomSeed seed ON seed.FoodName = f.FoodName AND seed.VariantName = v.VariantName AND seed.ItemName = i.ItemName;

INSERT INTO dbo.FoodIngredients (FoodId, VariantId, InventoryItemId, QuantityPerUnit, WastagePercent, CreatedAt)
SELECT f.FoodId, v.VariantId, i.InventoryItemId, seed.QuantityPerUnit, seed.WastagePercent, @Now
FROM @BomSeed seed
JOIN dbo.Foods f ON f.FoodName = seed.FoodName
JOIN dbo.FoodVariants v ON v.FoodId = f.FoodId AND v.VariantName = seed.VariantName
JOIN dbo.InventoryItems i ON i.ItemName = seed.ItemName
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.FoodIngredients x
    WHERE x.FoodId = f.FoodId AND x.VariantId = v.VariantId AND x.InventoryItemId = i.InventoryItemId
);

-- 6. Ke hoach suat mau hom nay. SystemRemain/VariancePortions la cot computed trong DB.
DECLARE @FoodName NVARCHAR(200), @VariantName NVARCHAR(100), @Planned DECIMAL(18,2);
DECLARE portion_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT FoodName, VariantName, PlannedPortions
FROM (VALUES
    (N'Ga Ta Hap La Chanh', N'1 phan', 20.0),
    (N'Ga Nuong Muoi Ot', N'1 phan', 20.0),
    (N'Lau Thai Chua Cay Hai San', N'Nho', 15.0),
    (N'Lau Nam Ga Ta Thao Moc', N'Nho', 15.0),
    (N'De Suon Bo My Nuong Sot Tieu', N'1 phan', 12.0),
    (N'Ba Chi Heo Nuong Sot Kim Chi', N'1 phan', 15.0),
    (N'Muc Trung Hap Gung Hanh', N'1 phan', 12.0),
    (N'Tom Su Nuong Muoi Ot', N'1 phan', 15.0),
    (N'Khoai Tay Chien Bo Toi', N'1 phan', 20.0),
    (N'Com Chien Hai San Hoang Kim', N'1 phan', 20.0)
) p(FoodName, VariantName, PlannedPortions);
OPEN portion_cursor;
FETCH NEXT FROM portion_cursor INTO @FoodName, @VariantName, @Planned;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @PortionFoodId INT = NULL, @PortionVariantId INT = NULL;
    SET @PortionFoodId = NULL;
    SET @PortionVariantId = NULL;
    SELECT @PortionFoodId = FoodId FROM dbo.Foods WHERE FoodName = @FoodName;
    SELECT @PortionVariantId = VariantId FROM dbo.FoodVariants WHERE FoodId = @PortionFoodId AND VariantName = @VariantName;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.DailyPortionControls
        WHERE ControlDate = @Today AND FoodId = @PortionFoodId AND VariantId = @PortionVariantId
    )
    BEGIN
        INSERT INTO dbo.DailyPortionControls
            (ControlDate, FoodId, VariantId, PlannedPortions, CarryOverPortions, CookedPortions, SoldPortions, ActualRemain, DisposedPortions, NextCarryOverPortions, Status, Note, CreatedAt, UpdatedAt)
        VALUES
            (@Today, @PortionFoodId, @PortionVariantId, @Planned, 0, 0, 0, NULL, 0, 0, 'planned', N'Ke hoach mau tu seed WMS/BOM', @Now, @Now);
    END

    FETCH NEXT FROM portion_cursor INTO @FoodName, @VariantName, @Planned;
END

CLOSE portion_cursor;
DEALLOCATE portion_cursor;

COMMIT TRANSACTION;
PRINT N'SEED WMS/BOM da hoan tat: 10 mon, 20 variant, BOM, lo kho, movement va ke hoach suat.';
GO

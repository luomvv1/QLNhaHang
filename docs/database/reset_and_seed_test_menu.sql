-- ============================================================================
-- DEV/TEST ONLY: RESET MENU AND CREATE A SMALL TEST MENU
--
-- This script permanently removes:
--   - menu categories, foods, variants and BOM rows
--   - daily portion controls
--   - order/invoice/payment test history that references the menu
--
-- It keeps users, customers, tables, suppliers, inventory items, batches,
-- stock receipts, stock movements and stock audits.
-- Do not run this script against production data.
-- ============================================================================

USE RestaurantManagement;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Now DATETIME2(0) = SYSDATETIME();
DECLARE @Today DATE = CAST(@Now AS DATE);
DECLARE @AdminUserId INT;

SELECT TOP (1) @AdminUserId = u.UserId
FROM dbo.Users u
JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1
  AND r.IsActive = 1
  AND r.RoleCode IN ('admin', 'manager')
ORDER BY CASE WHEN r.RoleCode = 'admin' THEN 0 ELSE 1 END, u.UserId;

IF @AdminUserId IS NULL
BEGIN
    THROW 51000, 'No active admin or manager user was found for the sample data.', 1;
END;

-- 1. Remove sales history that holds foreign keys to Foods/FoodVariants.
--    This keeps the database internally consistent after the menu reset.
DELETE FROM dbo.Payments;
DELETE FROM dbo.PromotionUsages;
DELETE FROM dbo.PointTransactions WHERE InvoiceId IS NOT NULL;
DELETE FROM dbo.Reviews WHERE InvoiceId IS NOT NULL;
DELETE FROM dbo.InvoiceItems;
DELETE FROM dbo.Invoices;
DELETE FROM dbo.OrderTables;
DELETE FROM dbo.OrderItems;
UPDATE dbo.Orders SET MergedIntoOrderId = NULL WHERE MergedIntoOrderId IS NOT NULL;
DELETE FROM dbo.Orders;

-- 2. Remove menu-dependent WMS/BOM records, then the menu itself.
DELETE FROM dbo.DailyPortionControls;
DELETE FROM dbo.FoodIngredients;
DELETE FROM dbo.FoodVariants;
DELETE FROM dbo.Foods;
DELETE FROM dbo.FoodCategories;

-- Reset only menu identities to make manual API testing easier.
DBCC CHECKIDENT ('dbo.FoodCategories', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Foods', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.FoodVariants', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.FoodIngredients', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.DailyPortionControls', RESEED, 0) WITH NO_INFOMSGS;

-- 3. Categories.
DECLARE @CategorySeed TABLE
(
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    SortOrder INT NOT NULL
);

INSERT INTO @CategorySeed VALUES
(N'Mon Chinh', N'Com, ga va cac mon an chinh de test order.', 1),
(N'Mon Lau', N'Lau nho va lau lon de test BOM theo variant.', 2),
(N'Mon Nuong', N'Mon nuong va hai san nuong.', 3),
(N'Mon An Kem', N'Khoai tay va mon an kem.', 4);

INSERT INTO dbo.FoodCategories
    (CategoryName, Description, SortOrder, IsActive, CreatedAt, UpdatedAt)
SELECT CategoryName, Description, SortOrder, 1, @Now, @Now
FROM @CategorySeed;

-- 4. Foods.
DECLARE @FoodSeed TABLE
(
    FoodName NVARCHAR(200) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(1000) NULL,
    MinPrice DECIMAL(18,2) NOT NULL
);

INSERT INTO @FoodSeed VALUES
(N'Com Chien Hai San Mini', N'Mon Chinh', N'Mon test co 2 variant va BOM nhieu nguyen lieu.', 129000),
(N'Ga Nuong Mat Ong', N'Mon Chinh', N'Mon test theo con ga va gia vi.', 189000),
(N'Lau Thai Nuoc Cot Dua', N'Mon Lau', N'Mon test FEFO voi tom, muc, nam va nuoc cot dua.', 299000),
(N'Muc Nuong Sa Te', N'Mon Nuong', N'Mon test hai san voi 2 co mo phan.', 219000),
(N'Khoai Tay Chien Lac Pho Mai', N'Mon An Kem', N'Mon an kem co dinh muc don gian.', 69000),
(N'Ga Ta Hap Nam', N'Mon Chinh', N'Mon test variant kich thuoc lon nho.', 179000);

INSERT INTO dbo.Foods
    (CategoryId, FoodName, Description, FoodStatus, MinPrice, IsActive, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
SELECT c.CategoryId, f.FoodName, f.Description, 'available', f.MinPrice, 1,
       @AdminUserId, @AdminUserId, @Now, @Now
FROM @FoodSeed f
JOIN dbo.FoodCategories c ON c.CategoryName = f.CategoryName;

-- 5. Variants.
DECLARE @VariantSeed TABLE
(
    FoodName NVARCHAR(200) NOT NULL,
    VariantName NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Unit NVARCHAR(30) NOT NULL,
    SortOrder INT NOT NULL
);

INSERT INTO @VariantSeed VALUES
(N'Com Chien Hai San Mini', N'1 suat', 129000, N'suat', 1),
(N'Com Chien Hai San Mini', N'2 suat', 229000, N'suat', 2),
(N'Ga Nuong Mat Ong', N'1 phan', 189000, N'phan', 1),
(N'Ga Nuong Mat Ong', N'2-3 nguoi', 349000, N'phan', 2),
(N'Lau Thai Nuoc Cot Dua', N'Nho', 299000, N'noi', 1),
(N'Lau Thai Nuoc Cot Dua', N'Lon', 459000, N'noi', 2),
(N'Muc Nuong Sa Te', N'1 phan', 219000, N'phan', 1),
(N'Muc Nuong Sa Te', N'2-3 nguoi', 399000, N'phan', 2),
(N'Khoai Tay Chien Lac Pho Mai', N'1 dia', 69000, N'dia', 1),
(N'Khoai Tay Chien Lac Pho Mai', N'2 dia', 119000, N'dia', 2),
(N'Ga Ta Hap Nam', N'1 phan', 179000, N'phan', 1),
(N'Ga Ta Hap Nam', N'2-3 nguoi', 329000, N'phan', 2);

INSERT INTO dbo.FoodVariants
    (FoodId, VariantName, Price, Unit, SortOrder, IsActive, CreatedAt, UpdatedAt)
SELECT f.FoodId, v.VariantName, v.Price, v.Unit, v.SortOrder, 1, @Now, @Now
FROM @VariantSeed v
JOIN dbo.Foods f ON f.FoodName = v.FoodName;

-- 6. Validate that the existing WMS sample inventory is available for the BOM.
DECLARE @RequiredItems TABLE (ItemName NVARCHAR(200) NOT NULL PRIMARY KEY);
INSERT INTO @RequiredItems VALUES
(N'Ga ta'), (N'Tom su'), (N'Muc ong'), (N'Nam kim cham'),
(N'Gao te'), (N'Khoai tay'), (N'Nuoc cot dua'), (N'Gia vi tong hop');

IF EXISTS
(
    SELECT 1
    FROM @RequiredItems r
    WHERE NOT EXISTS (SELECT 1 FROM dbo.InventoryItems i WHERE i.ItemName = r.ItemName)
)
BEGIN
    SELECT r.ItemName AS MissingInventoryItem
    FROM @RequiredItems r
    WHERE NOT EXISTS (SELECT 1 FROM dbo.InventoryItems i WHERE i.ItemName = r.ItemName);
    THROW 51001, 'Required inventory items are missing. Run the WMS inventory seed first.', 1;
END;

-- QuantityPerUnit is gross quantity in BaseUnit. WastagePercent is metadata
-- according to the current project convention and is not added a second time.
DECLARE @BomSeed TABLE
(
    FoodName NVARCHAR(200) NOT NULL,
    VariantName NVARCHAR(100) NOT NULL,
    ItemName NVARCHAR(200) NOT NULL,
    QuantityPerUnit DECIMAL(18,4) NOT NULL,
    WastagePercent DECIMAL(5,2) NOT NULL
);

INSERT INTO @BomSeed VALUES
(N'Com Chien Hai San Mini', N'1 suat', N'Gao te', 180, 2),
(N'Com Chien Hai San Mini', N'1 suat', N'Tom su', 50, 5),
(N'Com Chien Hai San Mini', N'1 suat', N'Muc ong', 50, 5),
(N'Com Chien Hai San Mini', N'1 suat', N'Gia vi tong hop', 12, 0),
(N'Com Chien Hai San Mini', N'2 suat', N'Gao te', 330, 2),
(N'Com Chien Hai San Mini', N'2 suat', N'Tom su', 90, 5),
(N'Com Chien Hai San Mini', N'2 suat', N'Muc ong', 90, 5),
(N'Com Chien Hai San Mini', N'2 suat', N'Gia vi tong hop', 20, 0),
(N'Ga Nuong Mat Ong', N'1 phan', N'Ga ta', 650, 8),
(N'Ga Nuong Mat Ong', N'1 phan', N'Gia vi tong hop', 25, 0),
(N'Ga Nuong Mat Ong', N'2-3 nguoi', N'Ga ta', 1200, 8),
(N'Ga Nuong Mat Ong', N'2-3 nguoi', N'Gia vi tong hop', 40, 0),
(N'Lau Thai Nuoc Cot Dua', N'Nho', N'Tom su', 180, 5),
(N'Lau Thai Nuoc Cot Dua', N'Nho', N'Muc ong', 180, 5),
(N'Lau Thai Nuoc Cot Dua', N'Nho', N'Nam kim cham', 120, 3),
(N'Lau Thai Nuoc Cot Dua', N'Nho', N'Nuoc cot dua', 250, 0),
(N'Lau Thai Nuoc Cot Dua', N'Nho', N'Gia vi tong hop', 20, 0),
(N'Lau Thai Nuoc Cot Dua', N'Lon', N'Tom su', 320, 5),
(N'Lau Thai Nuoc Cot Dua', N'Lon', N'Muc ong', 320, 5),
(N'Lau Thai Nuoc Cot Dua', N'Lon', N'Nam kim cham', 220, 3),
(N'Lau Thai Nuoc Cot Dua', N'Lon', N'Nuoc cot dua', 450, 0),
(N'Lau Thai Nuoc Cot Dua', N'Lon', N'Gia vi tong hop', 35, 0),
(N'Muc Nuong Sa Te', N'1 phan', N'Muc ong', 300, 5),
(N'Muc Nuong Sa Te', N'1 phan', N'Gia vi tong hop', 15, 0),
(N'Muc Nuong Sa Te', N'2-3 nguoi', N'Muc ong', 550, 5),
(N'Muc Nuong Sa Te', N'2-3 nguoi', N'Gia vi tong hop', 25, 0),
(N'Khoai Tay Chien Lac Pho Mai', N'1 dia', N'Khoai tay', 250, 3),
(N'Khoai Tay Chien Lac Pho Mai', N'1 dia', N'Gia vi tong hop', 8, 0),
(N'Khoai Tay Chien Lac Pho Mai', N'2 dia', N'Khoai tay', 450, 3),
(N'Khoai Tay Chien Lac Pho Mai', N'2 dia', N'Gia vi tong hop', 14, 0),
(N'Ga Ta Hap Nam', N'1 phan', N'Ga ta', 600, 8),
(N'Ga Ta Hap Nam', N'1 phan', N'Nam kim cham', 100, 3),
(N'Ga Ta Hap Nam', N'1 phan', N'Gia vi tong hop', 20, 0),
(N'Ga Ta Hap Nam', N'2-3 nguoi', N'Ga ta', 1100, 8),
(N'Ga Ta Hap Nam', N'2-3 nguoi', N'Nam kim cham', 180, 3),
(N'Ga Ta Hap Nam', N'2-3 nguoi', N'Gia vi tong hop', 32, 0);

INSERT INTO dbo.FoodIngredients
    (FoodId, VariantId, InventoryItemId, QuantityPerUnit, WastagePercent, CreatedAt)
SELECT f.FoodId, v.VariantId, i.InventoryItemId,
       b.QuantityPerUnit, b.WastagePercent, @Now
FROM @BomSeed b
JOIN dbo.Foods f ON f.FoodName = b.FoodName
JOIN dbo.FoodVariants v ON v.FoodId = f.FoodId AND v.VariantName = b.VariantName
JOIN dbo.InventoryItems i ON i.ItemName = b.ItemName;

-- 7. Sample daily plan for today. This makes the Daily Portions screen
-- immediately testable and allows Generate Requisition to create a draft.
DECLARE @PortionSeed TABLE
(
    FoodName NVARCHAR(200) NOT NULL,
    VariantName NVARCHAR(100) NOT NULL,
    PlannedPortions DECIMAL(18,2) NOT NULL
);

INSERT INTO @PortionSeed VALUES
(N'Com Chien Hai San Mini', N'1 suat', 20),
(N'Ga Nuong Mat Ong', N'1 phan', 12),
(N'Lau Thai Nuoc Cot Dua', N'Nho', 8),
(N'Muc Nuong Sa Te', N'1 phan', 15),
(N'Khoai Tay Chien Lac Pho Mai', N'1 dia', 20),
(N'Ga Ta Hap Nam', N'1 phan', 10);

INSERT INTO dbo.DailyPortionControls
    (ControlDate, FoodId, VariantId, PlannedPortions, CarryOverPortions,
     CookedPortions, SoldPortions, ActualRemain, DisposedPortions,
     NextCarryOverPortions, Status, Note, CreatedAt, UpdatedAt)
SELECT @Today, f.FoodId, v.VariantId, p.PlannedPortions, 0,
       0, 0, NULL, 0, 0, 'planned', N'Sample menu test plan', @Now, @Now
FROM @PortionSeed p
JOIN dbo.Foods f ON f.FoodName = p.FoodName
JOIN dbo.FoodVariants v ON v.FoodId = f.FoodId AND v.VariantName = p.VariantName;

COMMIT TRANSACTION;

SELECT 'FoodCategories' AS TableName, COUNT(*) AS TotalRows FROM dbo.FoodCategories
UNION ALL SELECT 'Foods', COUNT(*) FROM dbo.Foods
UNION ALL SELECT 'FoodVariants', COUNT(*) FROM dbo.FoodVariants
UNION ALL SELECT 'FoodIngredients', COUNT(*) FROM dbo.FoodIngredients
UNION ALL SELECT 'DailyPortionControls', COUNT(*) FROM dbo.DailyPortionControls;

PRINT 'TEST MENU RESET COMPLETE: 6 foods, 12 variants, 36 BOM rows and 6 daily plans.';
GO

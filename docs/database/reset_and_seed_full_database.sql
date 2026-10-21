-- ============================================================================
-- SCRIPT RESET TOÀN BỘ VÀ TẠO MỚI DỮ LIỆU ĐẦY ĐỦ CHO RESTAURANT MANAGEMENT
-- Bao gồm: Roles, Users, Customers, DiningTables, Suppliers,
--          InventoryItems (Đơn vị đơn & Quy đổi), InventoryBatches (FEFO),
--          FoodCategories, Foods, FoodVariants, FoodIngredients (BOM Recipes),
--          DailyPortionControls (Suất ăn hôm nay), Orders & OrderItems mẫu.
-- Tất cả tài khoản có mật khẩu đăng nhập: '123'
-- ============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'==> BẮT ĐẦU RESET TOÀN BỘ DATABASE RESTAURANT MANAGEMENT...';

-- 1. TẠM TẮT TOÀN BỘ RÀNG BUỘC KHÓA NGOẠI (FOREIGN KEYS)
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT ALL";
PRINT N'==> Đã tạm tắt toàn bộ Foreign Key constraints.';

-- 2. XÓA SẠCH DỮ LIỆU TẤT CẢ CÁC BẢNG THEO THỨ TỰ
BEGIN TRANSACTION;

DELETE FROM dbo.AiMessages;
DELETE FROM dbo.AiConversations;
DELETE FROM dbo.AuditLogs;

DELETE FROM dbo.StockAuditItems;
DELETE FROM dbo.StockAudits;

DELETE FROM dbo.StockReceiptDetails;
DELETE FROM dbo.StockReceipts;
DELETE FROM dbo.StockMovements;
DELETE FROM dbo.InventoryBatches;
DELETE FROM dbo.FoodIngredients;
DELETE FROM dbo.DailyPortionControls;
DELETE FROM dbo.InventoryItems;
DELETE FROM dbo.Suppliers;

DELETE FROM dbo.FoodVariants;
DELETE FROM dbo.Foods;
DELETE FROM dbo.FoodCategories;

DELETE FROM dbo.Payments;
DELETE FROM dbo.PromotionUsages;
DELETE FROM dbo.Promotions;
DELETE FROM dbo.PointTransactions;
DELETE FROM dbo.Reviews;
DELETE FROM dbo.InvoiceItems;
DELETE FROM dbo.Invoices;

DELETE FROM dbo.OrderTables;
DELETE FROM dbo.OrderItems;
UPDATE dbo.Orders SET MergedIntoOrderId = NULL;
DELETE FROM dbo.Orders;

DELETE FROM dbo.Reservations;
DELETE FROM dbo.DiningTables;
DELETE FROM dbo.Customers;
DELETE FROM dbo.Users;
DELETE FROM dbo.Roles;

PRINT N'==> Đã xóa toàn bộ dữ liệu cũ.';

-- 3. RESET LẠI IDENTITY VỀ 0 CHO TẤT CẢ CÁC BẢNG CÓ CỘT TỰ TĂNG
DBCC CHECKIDENT ('dbo.Roles', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Users', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Customers', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.DiningTables', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.FoodCategories', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Foods', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.FoodVariants', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.FoodIngredients', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Suppliers', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.InventoryItems', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.InventoryBatches', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.StockReceipts', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.StockReceiptDetails', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.StockMovements', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.DailyPortionControls', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.StockAudits', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.StockAuditItems', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Orders', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.OrderItems', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Invoices', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.InvoiceItems', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Payments', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Promotions', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Reservations', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.Reviews', RESEED, 0) WITH NO_INFOMSGS;

PRINT N'==> Đã reset identity về 0 thành công.';

DECLARE @Now DATETIME2(0) = SYSDATETIME();
DECLARE @Today DATE = CAST(@Now AS DATE);

-- ============================================================================
-- 4. TẠO DỮ LIỆU PHÂN QUYỀN & TÀI KHOẢN (ROLES & USERS)
-- ============================================================================
INSERT INTO dbo.Roles (RoleCode, RoleName, Description, IsActive) VALUES
('admin', N'Quản Trị Viên', N'Toàn quyền quản trị hệ thống, kho, phân quyền', 1),
('manager', N'Quản Lý Nhà Hàng', N'Quản lý doanh thu, kho, điều phối suất ăn, bàn', 1),
('chef', N'Bếp Trưởng / Bếp Phó', N'Quản lý suất ăn bếp, cập nhật món nấu, KDS', 1),
('staff', N'Nhân Viên Phục Vụ / Thu Ngân', N'Tạo đơn order, thanh toán hóa đơn, phục vụ bàn', 1),
('customer', N'Khách Hàng', N'Khách hàng thành viên đặt bàn, xem menu, tích điểm', 1);

-- Tất cả tài khoản mật khẩu đăng nhập: '123'
INSERT INTO dbo.Users (RoleId, FullName, Email, Phone, PasswordHash, AvatarUrl, IsActive, CreatedAt, UpdatedAt) VALUES
(1, N'Võ Văn Lượm (Admin)', 'admintest4@gmail.com', '0909000001', '123', 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150', 1, @Now, @Now),
(2, N'Nguyễn Quản Lý', 'manager@gmail.com', '0909000002', '123', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150', 1, @Now, @Now),
(3, N'Lê Bếp Trưởng', 'daubep1@gmail.com', '0909000003', '123', 'https://images.unsplash.com/photo-1577219491135-ce391730fb2c?w=150', 1, @Now, @Now),
(4, N'Trần Thu Ngân & Phục Vụ', 'nhanvien1@gmail.com', '0909000004', '123', 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150', 1, @Now, @Now),
(5, N'Hoàng Khách Thân Thiết', 'khach1@gmail.com', '0909000005', '123', 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150', 1, @Now, @Now);

-- Khách hàng thành viên
INSERT INTO dbo.Customers (UserId, FullName, Phone, Email, PointsBalance, TotalSpent, VisitCount, IsActive, CreatedAt, UpdatedAt) VALUES
(5, N'Hoàng Khách Thân Thiết', '0909000005', 'khach1@gmail.com', 250, 2500000, 5, 1, @Now, @Now),
(NULL, N'Đặng Mai Linh (Khách Mới)', '0908123456', 'khach2@gmail.com', 50, 500000, 1, 1, @Now, @Now);

-- ============================================================================
-- 5. TẠO DANH SÁCH BÀN ĂN (DINING TABLES)
-- ============================================================================
INSERT INTO dbo.DiningTables (TableCode, TableName, AreaName, Capacity, TableStatus, IsActive, CreatedAt, UpdatedAt) VALUES
('TB-01', N'Bàn 01 (Trong nhà)', N'Tầng 1 - Trong Nhà', 4, 'empty', 1, @Now, @Now),
('TB-02', N'Bàn 02 (Trong nhà)', N'Tầng 1 - Trong Nhà', 4, 'empty', 1, @Now, @Now),
('TB-03', N'Bàn 03 (Trong nhà)', N'Tầng 1 - Trong Nhà', 6, 'empty', 1, @Now, @Now),
('TB-04', N'Bàn 04 (Trong nhà)', N'Tầng 1 - Trong Nhà', 8, 'empty', 1, @Now, @Now),
('TB-05', N'Bàn 05 (Trong nhà)', N'Tầng 1 - Trong Nhà', 2, 'empty', 1, @Now, @Now),
('TB-06', N'Bàn 06 (Sân vườn)', N'Tầng 2 - Sân Vườn', 4, 'empty', 1, @Now, @Now),
('TB-07', N'Bàn 07 (Sân vườn)', N'Tầng 2 - Sân Vườn', 6, 'empty', 1, @Now, @Now),
('TB-08', N'Bàn 08 (Sân vườn)', N'Tầng 2 - Sân Vườn', 8, 'empty', 1, @Now, @Now),
('TB-09', N'Bàn VIP 01', N'Phòng VIP Lầu 2', 10, 'empty', 1, @Now, @Now),
('TB-10', N'Bàn VIP 02', N'Phòng VIP Lầu 2', 12, 'empty', 1, @Now, @Now);

-- ============================================================================
-- 6. TẠO NHÀ CUNG CẤP (SUPPLIERS)
-- ============================================================================
INSERT INTO dbo.Suppliers (SupplierName, Phone, Email, Address, IsActive, CreatedAt) VALUES
(N'Hải Sản Tươi Sống Tân Cảng', '0901000001', 'haisan@tancang.test', N'Cảng Tân Thuận, Quận 7, TP.HCM', 1, @Now),
(N'Thực Phẩm Tươi Sạch An Tâm', '0901000002', 'antam@thucpham.test', N'Số 88 Lý Thường Kiệt, Quận 10, TP.HCM', 1, @Now),
(N'Nông Sản & Rau Củ Xanh Đà Lạt', '0901000003', 'raucu@dalat.test', N'Xã Lạc Dương, TP. Đà Lạt, Lâm Đồng', 1, @Now),
(N'Gia Vị & Đồ Uống Kim Biên', '0901000004', 'giavi@kimbien.test', N'Chợ Kim Biên, Quận 5, TP.HCM', 1, @Now);

-- ============================================================================
-- 7. TẠO NGUYÊN LIỆU KHO (INVENTORY ITEMS) - ĐƠN VỊ ĐƠN & QUY ĐỔI
-- ============================================================================
-- Supplier 1: Hải sản | Supplier 2: Thịt | Supplier 3: Rau củ | Supplier 4: Gia vị, đồ uống
INSERT INTO dbo.InventoryItems 
    (SupplierId, ItemName, Unit, PurchaseUnit, BaseUnit, ConversionRate, CurrentQuantity, MinimumQuantity, CostPerUnit, IsActive, CreatedAt, UpdatedAt)
VALUES
-- ID 1: Thịt bò Mỹ (1 kg = 1000 g, giá mua 350.000đ/kg)
(2, N'Thịt Bò Mỹ', 'g', 'kg', 'g', 1000, 30000, 5000, 350000.00, 1, @Now, @Now),

-- ID 2: Gà ta thả vườn (Đơn vị đơn giản: con, Cost = 120.000đ/con)
(2, N'Gà Ta Thả Vườn', 'con', 'con', 'con', 1, 50, 10, 120000.00, 1, @Now, @Now),

-- ID 3: Tôm sú tươi (1 kg = 1000 g, giá mua 280.000đ/kg)
(1, N'Tôm Sú Tươi', 'g', 'kg', 'g', 1000, 40000, 5000, 280000.00, 1, @Now, @Now),

-- ID 4: Mực ống tươi (1 kg = 1000 g, giá mua 240.000đ/kg)
(1, N'Mực Ống Tươi', 'g', 'kg', 'g', 1000, 25000, 4000, 240000.00, 1, @Now, @Now),

-- ID 5: Mực trứng (1 kg = 1000 g, giá mua 220.000đ/kg)
(1, N'Mực Trứng', 'g', 'kg', 'g', 1000, 20000, 3000, 220000.00, 1, @Now, @Now),

-- ID 6: Ba chỉ heo rút sườn (1 kg = 1000 g, giá mua 160.000đ/kg)
(2, N'Ba Chỉ Heo Rút Sườn', 'g', 'kg', 'g', 1000, 25000, 5000, 160000.00, 1, @Now, @Now),

-- ID 7: Nấm kim châm (Đơn vị đơn: gói, Cost = 12.000đ/gói)
(3, N'Nấm Kim Châm', 'gói', 'gói', 'gói', 1, 80, 20, 12000.00, 1, @Now, @Now),

-- ID 8: Nước cốt dừa Thái (1 lon = 400 ml, giá mua 24.000đ/lon)
(4, N'Nước Cốt Dừa Thái', 'ml', 'lon', 'ml', 400, 6000, 1600, 24000.00, 1, @Now, @Now),

-- ID 9: Khoai tây cắt sẵn (1 kg = 1000 g, giá mua 45.000đ/kg)
(3, N'Khoai Tây Cắt Sẵn', 'g', 'kg', 'g', 1000, 30000, 5000, 45000.00, 1, @Now, @Now),

-- ID 10: Gạo thơm Jasmine (1 bao 25 kg, giá mua 600.000đ/bao)
(4, N'Gạo Thơm Jasmine', 'g', 'bao 25kg', 'g', 25000, 75000, 15000, 600000.00, 1, @Now, @Now),

-- ID 11: Bia Heineken (1 thùng = 24 lon, giá mua 432.000đ/thùng)
(4, N'Bia Heineken', 'lon', 'thùng', 'lon', 24, 144, 48, 432000.00, 1, @Now, @Now),

-- ID 12: Nước ngọt Coca Cola (1 thùng = 24 lon, giá mua 240.000đ/thùng)
(4, N'Nước Ngọt Coca Cola', 'lon', 'thùng', 'lon', 24, 144, 48, 240000.00, 1, @Now, @Now);

-- ============================================================================
-- 8. TẠO LÔ HÀNG CHUẨN FEFO (INVENTORY BATCHES)
-- Có lô cận hạn (3 ngày, 5 ngày) để test cảnh báo FEFO và lô dài hạn an toàn
-- ============================================================================
INSERT INTO dbo.InventoryBatches 
    (InventoryItemId, BatchCode, QuantityRemaining, OriginalQuantity, UnitCost, ManufacturingDate, ExpiryDate, ReceivedDate, SupplierId, Status, CreatedAt, UpdatedAt)
VALUES
-- Thịt bò: Lô 1 cận hạn (3 ngày), Lô 2 dài hạn (25 ngày)
(1, 'BAT-BOMY-FEFO1', 10000, 10000, 350000.00, DATEADD(day, -10, @Today), DATEADD(day, 3, @Today), @Now, 2, 'active', @Now, @Now),
(1, 'BAT-BOMY-FEFO2', 20000, 20000, 350000.00, DATEADD(day, -2, @Today), DATEADD(day, 25, @Today), @Now, 2, 'active', @Now, @Now),

-- Gà ta: Lô 1 cận hạn (5 ngày), Lô 2 dài hạn (20 ngày)
(2, 'BAT-GATA-FEFO1', 20, 20, 120000.00, DATEADD(day, -3, @Today), DATEADD(day, 5, @Today), @Now, 2, 'active', @Now, @Now),
(2, 'BAT-GATA-FEFO2', 30, 30, 120000.00, DATEADD(day, -1, @Today), DATEADD(day, 20, @Today), @Now, 2, 'active', @Now, @Now),

-- Tôm sú: Lô 1 cận hạn (6 ngày), Lô 2 dài hạn (30 ngày)
(3, 'BAT-TOMSU-FEFO1', 15000, 15000, 280000.00, DATEADD(day, -5, @Today), DATEADD(day, 6, @Today), @Now, 1, 'active', @Now, @Now),
(3, 'BAT-TOMSU-FEFO2', 25000, 25000, 280000.00, DATEADD(day, -1, @Today), DATEADD(day, 30, @Today), @Now, 1, 'active', @Now, @Now),

-- Mực ống: 25.000g (HSD 18 ngày)
(4, 'BAT-MUCONG-01', 25000, 25000, 240000.00, DATEADD(day, -2, @Today), DATEADD(day, 18, @Today), @Now, 1, 'active', @Now, @Now),

-- Mực trứng: 20.000g (HSD 20 ngày)
(5, 'BAT-MUCTRUNG-01', 20000, 20000, 220000.00, DATEADD(day, -2, @Today), DATEADD(day, 20, @Today), @Now, 1, 'active', @Now, @Now),

-- Ba chỉ heo: 25.000g (HSD 22 ngày)
(6, 'BAT-BACHI-01', 25000, 25000, 160000.00, DATEADD(day, -3, @Today), DATEADD(day, 22, @Today), @Now, 2, 'active', @Now, @Now),

-- Nấm kim châm: 80 gói (HSD 12 ngày)
(7, 'BAT-NAM-01', 80, 80, 12000.00, DATEADD(day, -2, @Today), DATEADD(day, 12, @Today), @Now, 3, 'active', @Now, @Now),

-- Nước cốt dừa: 6.000ml = 15 lon (HSD 90 ngày)
(8, 'BAT-COTDUA-01', 6000, 6000, 24000.00, DATEADD(day, -10, @Today), DATEADD(day, 90, @Today), @Now, 4, 'active', @Now, @Now),

-- Khoai tây cắt sẵn: 30.000g (HSD 45 ngày)
(9, 'BAT-KHOAITAY-01', 30000, 30000, 45000.00, DATEADD(day, -5, @Today), DATEADD(day, 45, @Today), @Now, 3, 'active', @Now, @Now),

-- Gạo thơm: 75.000g = 3 bao (HSD 180 ngày)
(10, 'BAT-GAO-01', 75000, 75000, 600000.00, DATEADD(day, -15, @Today), DATEADD(day, 180, @Today), @Now, 4, 'active', @Now, @Now),

-- Bia Heineken: 144 lon = 6 thùng (HSD 240 ngày)
(11, 'BAT-KEN-01', 144, 144, 432000.00, DATEADD(day, -20, @Today), DATEADD(day, 240, @Today), @Now, 4, 'active', @Now, @Now),

-- Coca Cola: 144 lon = 6 thùng (HSD 240 ngày)
(12, 'BAT-COCA-01', 144, 144, 240000.00, DATEADD(day, -20, @Today), DATEADD(day, 240, @Today), @Now, 4, 'active', @Now, @Now);

-- Tạo lịch sử StockMovements đầu kỳ
INSERT INTO dbo.StockMovements (InventoryItemId, BatchId, MovementType, Quantity, UnitCost, Reason, ReferenceType, ReferenceId, CreatedBy, CreatedAt)
SELECT b.InventoryItemId, b.BatchId, 'import', b.OriginalQuantity, b.UnitCost, N'Khởi tạo tồn kho ban đầu chuẩn FEFO', 'initial_setup', b.BatchId, 1, @Now
FROM dbo.InventoryBatches b;

-- Tạo 1 Phiếu nhập kho đã duyệt hoàn tất đầu kỳ
INSERT INTO dbo.StockReceipts (ReceiptCode, ReceiptType, Status, SupplierId, TotalAmount, Note, CreatedBy, ApprovedBy, ApprovedAt, CreatedAt, UpdatedAt)
VALUES ('PNK-SETUP-INITIAL', 'import_supplier', 'approved', 2, 45000000.00, N'Phiếu nhập kho đầu kỳ toàn bộ nguyên liệu mẫu', 1, 1, @Now, @Now, @Now);

-- ============================================================================
-- 9. TẠO THỰC ĐƠN MÓN ĂN & BIẾN THỂ (3 QUẦY BẾP TIÊU CHUẨN)
-- ============================================================================
INSERT INTO dbo.FoodCategories (CategoryName, Description, SortOrder, IsActive, CreatedAt, UpdatedAt) VALUES
(N'Quầy Nướng & Chiên', N'Chuyên món nướng than hoa, bò bít tết áp chảo, món chiên giòn', 1, 1, @Now, @Now),
(N'Quầy Nấu & Lẩu', N'Chuyên món lẩu hải sản, gà hấp lá chanh, cơm chiên chảo wok', 2, 1, @Now, @Now),
(N'Quầy Pha Chế & Đồ Uống', N'Chuyên bia lon ướp lạnh, nước ngọt có ga và đồ uống giải khát', 3, 1, @Now, @Now);

-- Foods (12 món ăn được phân chia hợp lý vào 3 quầy bếp)
INSERT INTO dbo.Foods (CategoryId, FoodName, Description, FoodStatus, MinPrice, ImageUrl, IsActive, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt) VALUES
-- Quầy 1: Nướng & Chiên (6 món nướng than hoa, áp chảo, chiên bơ tỏi)
(1, N'Bò Bít Tết Sốt Tiêu Đen', N'Thịt thăn bò Mỹ nướng mềm ngọt, hòa quyện cùng sốt tiêu đen Phú Quốc thơm lừng.', 'available', 139000, 'https://res.cloudinary.com/dvnfpynw5/image/upload/v1792127415/qloder_foods/qmdaim2jdzjommucj27d.jpg', 1, 1, 1, @Now, @Now),
(1, N'Ba Chỉ Heo Nướng Kim Chi', N'Ba chỉ heo rút sườn ướp sốt cay Hàn Quốc, nướng xém cạnh ăn kèm kim chi.', 'available', 169000, 'https://images.unsplash.com/photo-1544025162-d76694265947?w=700&auto=format&fit=crop&q=80', 1, 1, 1, @Now, @Now),
(2, N'Gà Ta Hấp Lá Chanh', N'Gà ta da vàng giòn sần sật, thịt ngọt chắc hấp thơm nồng mùi lá chanh.', 'available', 119000, 'https://images.unsplash.com/photo-1598103442097-8b74394b95c6?w=700&auto=format&fit=crop&q=80', 1, 1, 1, @Now, @Now),
(1, N'Gà Nướng Muối Ớt', N'Gà ta nướng than hoa vàng rộm, vị cay thơm đậm đà đặc trưng.', 'available', 129000, 'https://res.cloudinary.com/dvnfpynw5/image/upload/v1792127669/qloder_foods/dea60vv3avxcndjr3zjz.jpg', 1, 1, 1, @Now, @Now),
(2, N'Lẩu Thái Hải Sản Chua Cay', N'Nồi lẩu bốc khói nghi ngút với tôm sú, mực ống tươi, nấm và nước cốt dừa béo thơm.', 'available', 259000, 'https://res.cloudinary.com/dvnfpynw5/image/upload/v1792127718/qloder_foods/cmoj48uwhpunh34hhbwl.jpg', 1, 1, 1, @Now, @Now),
(2, N'Lẩu Nấm Gà Ta Thảo Mộc', N'Nước dùng gà hầm thanh ngọt tự nhiên kết hợp các loại nấm tươi bổ dưỡng.', 'available', 349000, 'https://images.unsplash.com/photo-1547592180-85f173990554?w=700&auto=format&fit=crop&q=80', 1, 1, 1, @Now, @Now),
(1, N'Tôm Sú Nướng Muối Ớt', N'Tôm sú tươi sống nướng muối ớt cay nồng, thịt ngọt giòn rụm.', 'available', 189000, 'https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?w=700&auto=format&fit=crop&q=80', 1, 1, 1, @Now, @Now),
(1, N'Mực Trứng Nướng Sa Tế', N'Mực trứng đầy ắp trứng bùi béo, nướng sa tế thơm cay hấp dẫn.', 'available', 189000, 'https://images.unsplash.com/photo-1599488615731-7e5c2823ff28?w=700&auto=format&fit=crop&q=80', 1, 1, 1, @Now, @Now),
(2, N'Cơm Chiên Hải Sản Hoàng Kim', N'Cơm chiên hạt tơi vàng óng ánh kết hợp tôm sú, mực giòn ngọt sần sật.', 'available', 89000, 'https://res.cloudinary.com/dvnfpynw5/image/upload/v1792127736/qloder_foods/glo97ts3b0g0wtyuqtec.jpg', 1, 1, 1, @Now, @Now),
(1, N'Khoai Tây Chiên Bơ Tỏi', N'Khoai tây chiên vàng giòn rụm lắc đều bơ tỏi thơm ngậy.', 'available', 59000, 'https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=700&auto=format&fit=crop&q=80', 1, 1, 1, @Now, @Now),
(3, N'Bia Heineken Ướp Lạnh', N'Bia Heineken lon 330ml ướp lạnh sảng khoái.', 'available', 28000, 'https://images.unsplash.com/photo-1618886614638-80e3c15cd819?w=700&auto=format&fit=crop&q=80', 1, 1, 1, @Now, @Now),
(3, N'Nước Ngọt Coca Cola', N'Nước giải khát có ga Coca Cola lon 330ml mát lạnh.', 'available', 18000, 'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=700&auto=format&fit=crop&q=80', 1, 1, 1, @Now, @Now);

-- Biến thể món ăn (FoodVariants)
INSERT INTO dbo.FoodVariants (FoodId, VariantName, Price, Unit, SortOrder, IsActive, CreatedAt, UpdatedAt) VALUES
-- Món 1: Bò Bít Tết (FoodId 1)
(1, N'Size Nhỏ (150g)', 139000, N'Phần', 1, 1, @Now, @Now),
(1, N'Size Vừa (250g)', 199000, N'Phần', 2, 1, @Now, @Now),
(1, N'Size Lớn (400g)', 299000, N'Phần', 3, 1, @Now, @Now),

-- Món 2: Ba Chỉ Heo (FoodId 2)
(2, N'Phần 250g', 169000, N'Đĩa', 1, 1, @Now, @Now),

-- Món 3: Gà Hấp (FoodId 3)
(3, N'Nửa Con', 119000, N'Phần', 1, 1, @Now, @Now),
(3, N'Nguyên Con', 229000, N'Con', 2, 1, @Now, @Now),

-- Món 4: Gà Nướng (FoodId 4)
(4, N'Nửa Con', 129000, N'Phần', 1, 1, @Now, @Now),
(4, N'Nguyên Con', 249000, N'Con', 2, 1, @Now, @Now),

-- Món 5: Lẩu Thái (FoodId 5)
(5, N'Nồi Nhỏ (2 người)', 259000, N'Nồi', 1, 1, @Now, @Now),
(5, N'Nồi Lớn (4 người)', 389000, N'Nồi', 2, 1, @Now, @Now),

-- Món 6: Lẩu Nấm (FoodId 6)
(6, N'Nồi Tiêu Chuẩn', 349000, N'Nồi', 1, 1, @Now, @Now),

-- Món 7: Tôm Sú Nướng (FoodId 7)
(7, N'Phần 300g', 189000, N'Đĩa', 1, 1, @Now, @Now),
(7, N'Phần 500g', 299000, N'Đĩa', 2, 1, @Now, @Now),

-- Món 8: Mực Trứng Nướng (FoodId 8)
(8, N'Đĩa Tiêu Chuẩn', 189000, N'Đĩa', 1, 1, @Now, @Now),

-- Món 9: Cơm Chiên (FoodId 9)
(9, N'Phần Nhỏ', 89000, N'Đĩa', 1, 1, @Now, @Now),
(9, N'Phần Lớn', 149000, N'Đĩa', 2, 1, @Now, @Now),

-- Món 10: Khoai Tây Chiên (FoodId 10)
(10, N'Phần Tiêu Chuẩn', 59000, N'Đĩa', 1, 1, @Now, @Now),

-- Món 11: Bia Heineken (FoodId 11)
(11, N'Lon 330ml', 28000, N'Lon', 1, 1, @Now, @Now),

-- Món 12: Coca Cola (FoodId 12)
(12, N'Lon 330ml', 18000, N'Lon', 1, 1, @Now, @Now);

-- ============================================================================
-- 10. TẠO ĐỊNH MỨC NGUYÊN LIỆU (BOM RECIPES - FOOD INGREDIENTS)
-- Cấu hình tỉ lệ định mức gross và hao hụt sơ chế theo đúng quy trình bếp
-- ============================================================================
INSERT INTO dbo.FoodIngredients (FoodId, VariantId, InventoryItemId, QuantityPerUnit, WastagePercent, CreatedAt) VALUES
-- Bò Bít Tết Size Nhỏ (Variant 1): 150g Thịt bò, 5% hao hụt
(1, 1, 1, 150.00, 5.0, @Now),
-- Bò Bít Tết Size Vừa (Variant 2): 250g Thịt bò, 5% hao hụt
(1, 2, 1, 250.00, 5.0, @Now),
-- Bò Bít Tết Size Lớn (Variant 3): 400g Thịt bò, 5% hao hụt
(1, 3, 1, 400.00, 5.0, @Now),

-- Ba Chỉ Heo (Variant 4): 250g Ba chỉ (Item 6), 3% hao hụt
(2, 4, 6, 250.00, 3.0, @Now),

-- Gà Hấp Nửa Con (Variant 5): 0.5 con gà (Item 2)
(3, 5, 2, 0.50, 0.0, @Now),
-- Gà Hấp Nguyên Con (Variant 6): 1.0 con gà (Item 2)
(3, 6, 2, 1.00, 0.0, @Now),

-- Gà Nướng Nửa Con (Variant 7): 0.5 con gà (Item 2)
(4, 7, 2, 0.50, 0.0, @Now),
-- Gà Nướng Nguyên Con (Variant 8): 1.0 con gà (Item 2)
(4, 8, 2, 1.00, 0.0, @Now),

-- Lẩu Thái Nồi Nhỏ (Variant 9): 200g Tôm sú (Item 3), 200g Mực ống (Item 4), 1 gói Nấm (Item 7), 200ml Cốt dừa (Item 8)
(5, 9, 3, 200.00, 3.0, @Now),
(5, 9, 4, 200.00, 3.0, @Now),
(5, 9, 7, 1.00, 0.0, @Now),
(5, 9, 8, 200.00, 0.0, @Now),

-- Lẩu Thái Nồi Lớn (Variant 10): 400g Tôm sú (Item 3), 400g Mực ống (Item 4), 2 gói Nấm (Item 7), 400ml Cốt dừa (Item 8)
(5, 10, 3, 400.00, 3.0, @Now),
(5, 10, 4, 400.00, 3.0, @Now),
(5, 10, 7, 2.00, 0.0, @Now),
(5, 10, 8, 400.00, 0.0, @Now),

-- Lẩu Nấm Gà (Variant 11): 0.8 con gà (Item 2), 2 gói Nấm (Item 7)
(6, 11, 2, 0.80, 0.0, @Now),
(6, 11, 7, 2.00, 0.0, @Now),

-- Tôm Sú 300g (Variant 12): 300g Tôm sú (Item 3), 3% hao hụt
(7, 12, 3, 300.00, 3.0, @Now),
-- Tôm Sú 500g (Variant 13): 500g Tôm sú (Item 3), 3% hao hụt
(7, 13, 3, 500.00, 3.0, @Now),

-- Mực Trứng Nướng (Variant 14): 250g Mực trứng (Item 5), 3% hao hụt
(8, 14, 5, 250.00, 3.0, @Now),

-- Cơm Chiên Nhỏ (Variant 15): 200g Gạo (Item 10), 80g Tôm sú (Item 3), 80g Mực ống (Item 4)
(9, 15, 10, 200.00, 2.0, @Now),
(9, 15, 3, 80.00, 3.0, @Now),
(9, 15, 4, 80.00, 3.0, @Now),

-- Cơm Chiên Lớn (Variant 16): 350g Gạo (Item 10), 150g Tôm sú (Item 3), 150g Mực ống (Item 4)
(9, 16, 10, 350.00, 2.0, @Now),
(9, 16, 3, 150.00, 3.0, @Now),
(9, 16, 4, 150.00, 3.0, @Now),

-- Khoai Tây Chiên (Variant 17): 200g Khoai tây (Item 9), 5% hao hụt
(10, 17, 9, 200.00, 5.0, @Now),

-- Bia Heineken (Variant 18): 1 lon bia (Item 11)
(11, 18, 11, 1.00, 0.0, @Now),

-- Coca Cola (Variant 19): 1 lon coca (Item 12)
(12, 19, 12, 1.00, 0.0, @Now);

-- ============================================================================
-- 11. TẠO BẢNG ĐIỀU PHỐI SUẤT ĂN HÔM NAY (DAILY PORTION CONTROLS)
-- Có sẵn tiến độ nấu và bán để test KDS và Tồn Bếp Realtime
-- ============================================================================
INSERT INTO dbo.DailyPortionControls 
    (ControlDate, FoodId, VariantId, PlannedPortions, CarryOverPortions, CookedPortions, SoldPortions, ActualRemain, DisposedPortions, NextCarryOverPortions, Status, Note, CreatedAt, UpdatedAt)
VALUES
-- Bò Bít Tết (Vừa): Planned 20, Tồn hôm qua 2, Đã nấu 15, Đã bán 10 -> Tồn bếp: 5
(@Today, 1, 2, 20.00, 2.00, 15.00, 10.00, 0.00, 0.00, 0.00, 'in_service', N'Món chạy tốt bữa trưa', @Now, @Now),

-- Lẩu Thái (Nồi Lớn): Planned 15, Tồn hôm qua 0, Đã nấu 10, Đã bán 6 -> Tồn bếp: 4
(@Today, 5, 10, 15.00, 0.00, 10.00, 6.00, 0.00, 0.00, 0.00, 'in_service', N'Chuẩn bị cho bàn đặt chiều', @Now, @Now),

-- Gà Nướng Muối Ớt (Nguyên Con): Planned 12, Tồn hôm qua 1, Đã nấu 8, Đã bán 5 -> Tồn bếp: 3
(@Today, 4, 8, 12.00, 1.00, 8.00, 5.00, 0.00, 0.00, 0.00, 'in_service', N'Ướp sẵn trước 2 con', @Now, @Now),

-- Tôm Sú Nướng (300g): Planned 18, Tồn hôm qua 0, Đã nấu 12, Đã bán 8 -> Tồn bếp: 4
(@Today, 7, 12, 18.00, 0.00, 12.00, 8.00, 0.00, 0.00, 0.00, 'in_service', N'Hải sản tươi giao sáng', @Now, @Now),

-- Khoai Tây Chiên: Planned 25, Tồn hôm qua 3, Đã nấu 20, Đã bán 14 -> Tồn bếp: 6
(@Today, 10, 17, 25.00, 3.00, 20.00, 14.00, 0.00, 0.00, 0.00, 'in_service', N'Món ăn vặt gọi nhiều', @Now, @Now),

-- Cơm Chiên Hải Sản (Lớn): Planned 16, Tồn hôm qua 0, Đã nấu 10, Đã bán 7 -> Tồn bếp: 3
(@Today, 9, 16, 16.00, 0.00, 10.00, 7.00, 0.00, 0.00, 0.00, 'in_service', N'Chuẩn bị sốt hoàng kim', @Now, @Now);

-- ============================================================================
-- 12. TẠO 2 ORDER ĐANG PHỤC VỤ MẪU TẠI BÀN 1 VÀ BÀN 2 ĐỂ TEST 3 QUẦY KDS BẾP
-- ============================================================================
-- ORDER 1 TẠI BÀN 1 (TB-01)
INSERT INTO dbo.Orders 
    (CustomerId, CreatedBy, OrderStatus, Subtotal, DiscountAmount, TotalAmount, Note, CreatedAt, UpdatedAt)
VALUES
(1, 4, 'serving', 615000, 0, 615000, N'Khách gọi thêm sốt tiêu đen', @Now, @Now);

INSERT INTO dbo.OrderTables (OrderId, TableId, IsPrimary, AssignedAt) VALUES (1, 1, 1, @Now);
UPDATE dbo.DiningTables SET TableStatus = 'serving', UpdatedAt = @Now WHERE TableId = 1;

-- Chi tiết món Order 1: Có đủ 3 quầy bếp
INSERT INTO dbo.OrderItems 
    (OrderId, FoodId, VariantId, FoodNameSnapshot, VariantSnapshot, UnitPrice, Quantity, Note, ItemStatus, CreatedBy, CreatedAt, UpdatedAt)
VALUES
-- Quầy 1: Nướng & Chiên (Đang nấu)
(1, 1, 2, N'Bò Bít Tết Sốt Tiêu Đen', N'Size Vừa (250g)', 199000, 1, N'Chín vừa (Medium rare)', 'cooking', 4, @Now, @Now),
-- Quầy 2: Nấu & Lẩu (Chờ làm)
(1, 5, 10, N'Lẩu Thái Hải Sản Chua Cay', N'Nồi Lớn (4 người)', 389000, 1, N'Cay vừa', 'confirmed', 4, @Now, @Now),
-- Quầy 3: Pha Chế & Đồ Uống (Chờ làm)
(1, 11, 18, N'Bia Heineken Ướp Lạnh', N'Lon 330ml', 28000, 1, N'Ướp lạnh không đá', 'confirmed', 4, @Now, @Now);

-- ORDER 2 TẠI BÀN 2 (TB-02)
INSERT INTO dbo.Orders 
    (CustomerId, CreatedBy, OrderStatus, Subtotal, DiscountAmount, TotalAmount, Note, CreatedAt, UpdatedAt)
VALUES
(2, 4, 'serving', 276000, 0, 276000, N'Bàn sinh nhật khách quen', @Now, @Now);

INSERT INTO dbo.OrderTables (OrderId, TableId, IsPrimary, AssignedAt) VALUES (2, 2, 1, @Now);
UPDATE dbo.DiningTables SET TableStatus = 'serving', UpdatedAt = @Now WHERE TableId = 2;

-- Chi tiết món Order 2: Có đủ 3 quầy bếp
INSERT INTO dbo.OrderItems 
    (OrderId, FoodId, VariantId, FoodNameSnapshot, VariantSnapshot, UnitPrice, Quantity, Note, ItemStatus, CreatedBy, CreatedAt, UpdatedAt)
VALUES
-- Quầy 1: Nướng & Chiên (Chờ làm)
(2, 2, 4, N'Ba Chỉ Heo Nướng Kim Chi', N'Phần 250g', 169000, 1, N'Nướng giòn cạnh', 'confirmed', 4, @Now, @Now),
-- Quầy 2: Nấu & Lẩu (Đang nấu)
(2, 9, 16, N'Cơm Chiên Hải Sản Hoàng Kim', N'Dĩa Lớn (3-4 người)', 89000, 1, N'Ít hành lá', 'cooking', 4, @Now, @Now),
-- Quầy 3: Pha Chế & Đồ Uống (Đã chuẩn bị xong, chờ bưng)
(2, 12, 19, N'Nước Ngọt Coca Cola', N'Lon 330ml', 18000, 1, N'Cho 2 ly đá riêng', 'ready', 4, @Now, @Now);

COMMIT TRANSACTION;

-- ============================================================================
-- 13. BẬT LẠI TOÀN BỘ RÀNG BUỘC KHÓA NGOẠI (FOREIGN KEYS) VÀ KIỂM TRA
-- ============================================================================
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL";
PRINT N'==> Đã bật lại toàn bộ Foreign Key constraints thành công!';

PRINT N'====================================================================';
PRINT N'==> THÀNH CÔNG: ĐÃ RESET VÀ TẠO MỚI DỮ LIỆU ĐẦY ĐỦ CHO RESTAURANT MANAGEMENT!';
PRINT N'    - 3 Quầy bếp tiêu chuẩn: Quầy Nướng & Chiên, Quầy Nấu & Lẩu, Quầy Pha Chế & Đồ Uống';
PRINT N'    - 12 Món ăn được chia chuẩn theo 3 quầy bếp';
PRINT N'    - 2 Order mẫu tại Bàn 1 và Bàn 2 (mỗi bàn đều có món từ cả 3 quầy)';
PRINT N'====================================================================';

/*
  SQL Server schema for the restaurant management system.
  Run this script in a new database after reviewing the names and rules.
*/

IF DB_ID(N'RestaurantManagement') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE RestaurantManagement');
END;
GO

USE RestaurantManagement;
GO

/* =========================
   Accounts and permissions
   ========================= */
CREATE TABLE dbo.Roles (
    RoleId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    RoleCode     VARCHAR(30) NOT NULL CONSTRAINT UQ_Roles_RoleCode UNIQUE,
    RoleName     NVARCHAR(100) NOT NULL,
    Description  NVARCHAR(500) NULL,
    IsActive     BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT 1
);
GO

CREATE TABLE dbo.Users (
    UserId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    RoleId        INT NOT NULL,
    FullName      NVARCHAR(150) NOT NULL,
    Email         VARCHAR(255) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    Phone         VARCHAR(20) NULL,
    PasswordHash  VARCHAR(500) NULL,
    AvatarUrl     VARCHAR(1000) NULL,
    IsActive      BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    LastLoginAt   DATETIME2(0) NULL,
    CreatedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);
GO

/* =========================
   Menu
   ========================= */
CREATE TABLE dbo.FoodCategories (
    CategoryId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FoodCategories PRIMARY KEY,
    CategoryName  NVARCHAR(100) NOT NULL CONSTRAINT UQ_FoodCategories_Name UNIQUE,
    Description   NVARCHAR(500) NULL,
    SortOrder     INT NOT NULL CONSTRAINT DF_FoodCategories_SortOrder DEFAULT 0,
    IsActive      BIT NOT NULL CONSTRAINT DF_FoodCategories_IsActive DEFAULT 1,
    CreatedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_FoodCategories_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_FoodCategories_UpdatedAt DEFAULT SYSDATETIME()
);
GO

CREATE TABLE dbo.Foods (
    FoodId              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Foods PRIMARY KEY,
    CategoryId          INT NOT NULL,
    FoodName            NVARCHAR(200) NOT NULL,
    Description         NVARCHAR(1000) NULL,
    ImageUrl            VARCHAR(1000) NULL,
    FoodStatus          VARCHAR(20) NOT NULL CONSTRAINT DF_Foods_Status DEFAULT 'available',
    MinPrice            DECIMAL(18,2) NOT NULL CONSTRAINT DF_Foods_MinPrice DEFAULT 0,
    IsActive            BIT NOT NULL CONSTRAINT DF_Foods_IsActive DEFAULT 1,
    CreatedBy           INT NULL,
    UpdatedBy           INT NULL,
    CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Foods_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Foods_UpdatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Foods_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.FoodCategories(CategoryId),
    CONSTRAINT FK_Foods_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Foods_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Foods_Status CHECK (FoodStatus IN ('available','unavailable','sold_out','hidden')),
    CONSTRAINT CK_Foods_MinPrice CHECK (MinPrice >= 0)
);
GO

CREATE TABLE dbo.FoodVariants (
    VariantId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FoodVariants PRIMARY KEY,
    FoodId       INT NOT NULL,
    VariantName  NVARCHAR(100) NOT NULL,
    Price        DECIMAL(18,2) NOT NULL,
    Unit         NVARCHAR(30) NOT NULL,
    SortOrder    INT NOT NULL CONSTRAINT DF_FoodVariants_SortOrder DEFAULT 0,
    IsActive     BIT NOT NULL CONSTRAINT DF_FoodVariants_IsActive DEFAULT 1,
    CreatedAt    DATETIME2(0) NOT NULL CONSTRAINT DF_FoodVariants_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt    DATETIME2(0) NOT NULL CONSTRAINT DF_FoodVariants_UpdatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_FoodVariants_Foods FOREIGN KEY (FoodId) REFERENCES dbo.Foods(FoodId),
    CONSTRAINT CK_FoodVariants_Price CHECK (Price >= 0),
    CONSTRAINT UQ_FoodVariants_Food_Name UNIQUE (FoodId, VariantName)
);
GO

/* =========================
   Customers, tables, reservations
   ========================= */
CREATE TABLE dbo.Customers (
    CustomerId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    UserId              INT NULL CONSTRAINT UQ_Customers_User UNIQUE,
    FullName            NVARCHAR(150) NOT NULL,
    Phone               VARCHAR(20) NOT NULL CONSTRAINT UQ_Customers_Phone UNIQUE,
    Email               VARCHAR(255) NULL,
    AvatarUrl           VARCHAR(1000) NULL,
    PointsBalance       INT NOT NULL CONSTRAINT DF_Customers_Points DEFAULT 0,
    TotalSpent          DECIMAL(18,2) NOT NULL CONSTRAINT DF_Customers_TotalSpent DEFAULT 0,
    VisitCount          INT NOT NULL CONSTRAINT DF_Customers_VisitCount DEFAULT 0,
    IsActive            BIT NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT 1,
    CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Customers_UpdatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Customers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Customers_Points CHECK (PointsBalance >= 0),
    CONSTRAINT CK_Customers_TotalSpent CHECK (TotalSpent >= 0)
);
GO

CREATE TABLE dbo.DiningTables (
    TableId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DiningTables PRIMARY KEY,
    TableCode      VARCHAR(30) NOT NULL CONSTRAINT UQ_DiningTables_Code UNIQUE,
    TableName      NVARCHAR(100) NOT NULL,
    AreaName       NVARCHAR(100) NULL,
    Capacity       INT NOT NULL CONSTRAINT DF_DiningTables_Capacity DEFAULT 4,
    TableStatus    VARCHAR(20) NOT NULL CONSTRAINT DF_DiningTables_Status DEFAULT 'empty',
    IsActive       BIT NOT NULL CONSTRAINT DF_DiningTables_IsActive DEFAULT 1,
    CreatedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_DiningTables_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_DiningTables_UpdatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT CK_DiningTables_Capacity CHECK (Capacity > 0),
    CONSTRAINT CK_DiningTables_Status CHECK (TableStatus IN ('empty','serving','reserved','cleaning','disabled'))
);
GO

CREATE TABLE dbo.Reservations (
    ReservationId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reservations PRIMARY KEY,
    CustomerId        INT NOT NULL,
    TableId           INT NULL,
    CustomerName      NVARCHAR(150) NOT NULL,
    CustomerPhone     VARCHAR(20) NOT NULL,
    ReservationDate   DATETIME2(0) NOT NULL,
    GuestCount        INT NOT NULL,
    Note              NVARCHAR(1000) NULL,
    ReservationStatus VARCHAR(20) NOT NULL CONSTRAINT DF_Reservations_Status DEFAULT 'pending',
    ConfirmedBy       INT NULL,
    CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_Reservations_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_Reservations_UpdatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Reservations_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT FK_Reservations_Tables FOREIGN KEY (TableId) REFERENCES dbo.DiningTables(TableId),
    CONSTRAINT FK_Reservations_ConfirmedBy FOREIGN KEY (ConfirmedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Reservations_GuestCount CHECK (GuestCount > 0),
    CONSTRAINT CK_Reservations_Status CHECK (ReservationStatus IN ('pending','confirmed','checked_in','completed','cancelled','rejected','no_show'))
);
GO

/* =========================
   Orders and kitchen
   ========================= */
CREATE TABLE dbo.Orders (
    OrderId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId       INT NULL,
    CreatedBy        INT NOT NULL,
    OrderStatus      VARCHAR(20) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT 'open',
    Subtotal         DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Subtotal DEFAULT 0,
    DiscountAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Discount DEFAULT 0,
    TotalAmount      DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Total DEFAULT 0,
    Note             NVARCHAR(1000) NULL,
    MergedIntoOrderId INT NULL,
    CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_Orders_UpdatedAt DEFAULT SYSDATETIME(),
    PaidAt           DATETIME2(0) NULL,
    CancelledAt      DATETIME2(0) NULL,
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT FK_Orders_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Orders_MergedInto FOREIGN KEY (MergedIntoOrderId) REFERENCES dbo.Orders(OrderId),
    CONSTRAINT CK_Orders_Status CHECK (OrderStatus IN ('open','preparing','serving','completed','paid','cancelled','merged')),
    CONSTRAINT CK_Orders_Amounts CHECK (Subtotal >= 0 AND DiscountAmount >= 0 AND TotalAmount >= 0)
);
GO

CREATE TABLE dbo.OrderTables (
    OrderId       INT NOT NULL,
    TableId       INT NOT NULL,
    IsPrimary     BIT NOT NULL CONSTRAINT DF_OrderTables_IsPrimary DEFAULT 0,
    AssignedAt    DATETIME2(0) NOT NULL CONSTRAINT DF_OrderTables_AssignedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_OrderTables PRIMARY KEY (OrderId, TableId),
    CONSTRAINT FK_OrderTables_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId),
    CONSTRAINT FK_OrderTables_Tables FOREIGN KEY (TableId) REFERENCES dbo.DiningTables(TableId)
);
GO

CREATE TABLE dbo.OrderItems (
    OrderItemId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
    OrderId           INT NOT NULL,
    FoodId            INT NOT NULL,
    VariantId         INT NOT NULL,
    FoodNameSnapshot  NVARCHAR(200) NOT NULL,
    VariantSnapshot   NVARCHAR(100) NOT NULL,
    UnitPrice         DECIMAL(18,2) NOT NULL,
    Quantity          INT NOT NULL,
    LineTotal         AS (UnitPrice * Quantity) PERSISTED,
    Note              NVARCHAR(1000) NULL,
    ItemStatus        VARCHAR(20) NOT NULL CONSTRAINT DF_OrderItems_Status DEFAULT 'pending',
    CreatedBy         INT NOT NULL,
    ConfirmedBy       INT NULL,
    ChefId            INT NULL,
    CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_OrderItems_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_OrderItems_UpdatedAt DEFAULT SYSDATETIME(),
    ConfirmedAt       DATETIME2(0) NULL,
    ReadyAt           DATETIME2(0) NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId),
    CONSTRAINT FK_OrderItems_Foods FOREIGN KEY (FoodId) REFERENCES dbo.Foods(FoodId),
    CONSTRAINT FK_OrderItems_Variants FOREIGN KEY (VariantId) REFERENCES dbo.FoodVariants(VariantId),
    CONSTRAINT FK_OrderItems_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_OrderItems_ConfirmedBy FOREIGN KEY (ConfirmedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_OrderItems_Chef FOREIGN KEY (ChefId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderItems_Price CHECK (UnitPrice >= 0),
    CONSTRAINT CK_OrderItems_Status CHECK (ItemStatus IN ('pending','confirmed','cooking','ready','served','cancelled'))
);
GO

/* =========================
   Promotions and payment
   ========================= */
CREATE TABLE dbo.Promotions (
    PromotionId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Promotions PRIMARY KEY,
    PromotionCode     VARCHAR(50) NOT NULL CONSTRAINT UQ_Promotions_Code UNIQUE,
    PromotionName     NVARCHAR(200) NOT NULL,
    PromotionType     VARCHAR(30) NOT NULL,
    PromotionValue    DECIMAL(18,2) NOT NULL,
    MinimumOrderValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_Promotions_MinOrder DEFAULT 0,
    UsageLimit        INT NULL,
    UsedCount         INT NOT NULL CONSTRAINT DF_Promotions_UsedCount DEFAULT 0,
    StartAt           DATETIME2(0) NOT NULL,
    EndAt             DATETIME2(0) NOT NULL,
    IsActive          BIT NOT NULL CONSTRAINT DF_Promotions_IsActive DEFAULT 1,
    CreatedBy         INT NOT NULL,
    CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_Promotions_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_Promotions_UpdatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Promotions_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Promotions_Type CHECK (PromotionType IN ('percent','fixed','point_multiplier')),
    CONSTRAINT CK_Promotions_Dates CHECK (EndAt > StartAt),
    CONSTRAINT CK_Promotions_Value CHECK (PromotionValue >= 0)
);
GO

CREATE TABLE dbo.Invoices (
    InvoiceId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
    InvoiceCode      VARCHAR(30) NOT NULL CONSTRAINT UQ_Invoices_Code UNIQUE,
    OrderId          INT NOT NULL CONSTRAINT UQ_Invoices_Order UNIQUE,
    CustomerId       INT NULL,
    IssuedBy         INT NOT NULL,
    Subtotal         DECIMAL(18,2) NOT NULL,
    DiscountAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Discount DEFAULT 0,
    PointsUsed       INT NOT NULL CONSTRAINT DF_Invoices_PointsUsed DEFAULT 0,
    PointsEarned     INT NOT NULL CONSTRAINT DF_Invoices_PointsEarned DEFAULT 0,
    TotalAmount      DECIMAL(18,2) NOT NULL,
    InvoiceStatus    VARCHAR(20) NOT NULL CONSTRAINT DF_Invoices_Status DEFAULT 'paid',
    IssuedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_Invoices_IssuedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Invoices_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId),
    CONSTRAINT FK_Invoices_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT FK_Invoices_IssuedBy FOREIGN KEY (IssuedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Invoices_Status CHECK (InvoiceStatus IN ('paid','refunded','cancelled')),
    CONSTRAINT CK_Invoices_Amounts CHECK (Subtotal >= 0 AND DiscountAmount >= 0 AND TotalAmount >= 0)
);
GO

CREATE TABLE dbo.InvoiceItems (
    InvoiceItemId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InvoiceItems PRIMARY KEY,
    InvoiceId         INT NOT NULL,
    FoodId            INT NULL,
    VariantId         INT NULL,
    FoodNameSnapshot  NVARCHAR(200) NOT NULL,
    VariantSnapshot   NVARCHAR(100) NOT NULL,
    UnitPrice         DECIMAL(18,2) NOT NULL,
    Quantity          INT NOT NULL,
    LineTotal         DECIMAL(18,2) NOT NULL,
    Note              NVARCHAR(1000) NULL,
    CONSTRAINT FK_InvoiceItems_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT FK_InvoiceItems_Foods FOREIGN KEY (FoodId) REFERENCES dbo.Foods(FoodId),
    CONSTRAINT FK_InvoiceItems_Variants FOREIGN KEY (VariantId) REFERENCES dbo.FoodVariants(VariantId),
    CONSTRAINT CK_InvoiceItems_Quantity CHECK (Quantity > 0)
);
GO

CREATE TABLE dbo.Payments (
    PaymentId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    InvoiceId        INT NOT NULL,
    PaymentMethod    VARCHAR(30) NOT NULL,
    Amount           DECIMAL(18,2) NOT NULL,
    TransactionCode  VARCHAR(100) NULL,
    PaymentStatus    VARCHAR(20) NOT NULL CONSTRAINT DF_Payments_Status DEFAULT 'completed',
    PaidAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Payments_PaidAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Payments_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT CK_Payments_Method CHECK (PaymentMethod IN ('cash','transfer','card','e_wallet')),
    CONSTRAINT CK_Payments_Status CHECK (PaymentStatus IN ('pending','completed','failed','refunded')),
    CONSTRAINT CK_Payments_Amount CHECK (Amount >= 0)
);
GO

CREATE TABLE dbo.PromotionUsages (
    PromotionUsageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PromotionUsages PRIMARY KEY,
    PromotionId      INT NOT NULL,
    InvoiceId        INT NOT NULL,
    CustomerId       INT NULL,
    DiscountAmount   DECIMAL(18,2) NOT NULL,
    UsedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_PromotionUsages_UsedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_PromotionUsages_Promotions FOREIGN KEY (PromotionId) REFERENCES dbo.Promotions(PromotionId),
    CONSTRAINT FK_PromotionUsages_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT FK_PromotionUsages_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT UQ_PromotionUsages_Invoice UNIQUE (PromotionId, InvoiceId)
);
GO

/* =========================
   Inventory
   ========================= */
CREATE TABLE dbo.Suppliers (
    SupplierId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    SupplierName  NVARCHAR(200) NOT NULL,
    Phone         VARCHAR(20) NULL,
    Email         VARCHAR(255) NULL,
    Address       NVARCHAR(500) NULL,
    IsActive      BIT NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT 1,
    CreatedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_Suppliers_CreatedAt DEFAULT SYSDATETIME()
);
GO

CREATE TABLE dbo.InventoryItems (
    InventoryItemId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryItems PRIMARY KEY,
    SupplierId       INT NULL,
    ItemName         NVARCHAR(200) NOT NULL,
    Unit             NVARCHAR(30) NOT NULL,
    CurrentQuantity  DECIMAL(18,3) NOT NULL CONSTRAINT DF_InventoryItems_Quantity DEFAULT 0,
    MinimumQuantity  DECIMAL(18,3) NOT NULL CONSTRAINT DF_InventoryItems_Minimum DEFAULT 0,
    CostPerUnit      DECIMAL(18,2) NOT NULL CONSTRAINT DF_InventoryItems_Cost DEFAULT 0, -- Gia theo PurchaseUnit
    IsActive         BIT NOT NULL CONSTRAINT DF_InventoryItems_IsActive DEFAULT 1,
    CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_InventoryItems_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_InventoryItems_UpdatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_InventoryItems_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(SupplierId),
    CONSTRAINT CK_InventoryItems_Quantity CHECK (CurrentQuantity >= 0 AND MinimumQuantity >= 0)
);
GO

CREATE TABLE dbo.FoodIngredients (
    FoodId            INT NOT NULL,
    InventoryItemId   INT NOT NULL,
    QuantityPerUnit   DECIMAL(18,3) NOT NULL,
    CONSTRAINT PK_FoodIngredients PRIMARY KEY (FoodId, InventoryItemId),
    CONSTRAINT FK_FoodIngredients_Foods FOREIGN KEY (FoodId) REFERENCES dbo.Foods(FoodId),
    CONSTRAINT FK_FoodIngredients_Inventory FOREIGN KEY (InventoryItemId) REFERENCES dbo.InventoryItems(InventoryItemId),
    CONSTRAINT CK_FoodIngredients_Quantity CHECK (QuantityPerUnit > 0)
);
GO

CREATE TABLE dbo.StockMovements (
    StockMovementId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockMovements PRIMARY KEY,
    InventoryItemId INT NOT NULL,
    MovementType     VARCHAR(20) NOT NULL,
    Quantity         DECIMAL(18,3) NOT NULL,
    UnitCost         DECIMAL(18,2) NULL, -- Gia theo PurchaseUnit
    Reason           NVARCHAR(500) NULL,
    ReferenceType    VARCHAR(30) NULL,
    ReferenceId      INT NULL,
    CreatedBy        INT NOT NULL,
    CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_StockMovements_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_StockMovements_Inventory FOREIGN KEY (InventoryItemId) REFERENCES dbo.InventoryItems(InventoryItemId),
    CONSTRAINT FK_StockMovements_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_StockMovements_Type CHECK (MovementType IN ('import','export','adjustment','waste')),
    CONSTRAINT CK_StockMovements_Quantity CHECK (Quantity > 0)
);
GO

/* =========================
   Loyalty, reviews and AI
   ========================= */
CREATE TABLE dbo.PointTransactions (
    PointTransactionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PointTransactions PRIMARY KEY,
    CustomerId         INT NOT NULL,
    InvoiceId          INT NULL,
    TransactionType    VARCHAR(20) NOT NULL,
    Points             INT NOT NULL,
    BalanceAfter      INT NOT NULL,
    Note               NVARCHAR(500) NULL,
    CreatedBy          INT NULL,
    CreatedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_PointTransactions_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_PointTransactions_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT FK_PointTransactions_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT FK_PointTransactions_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_PointTransactions_Type CHECK (TransactionType IN ('earn','redeem','adjustment','refund')),
    CONSTRAINT CK_PointTransactions_Balance CHECK (BalanceAfter >= 0)
);
GO

CREATE TABLE dbo.Reviews (
    ReviewId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reviews PRIMARY KEY,
    CustomerId     INT NOT NULL,
    InvoiceId      INT NULL,
    Rating         TINYINT NOT NULL,
    Comment        NVARCHAR(2000) NULL,
    Reply          NVARCHAR(2000) NULL,
    IsVisible      BIT NOT NULL CONSTRAINT DF_Reviews_IsVisible DEFAULT 1,
    CreatedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_Reviews_UpdatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Reviews_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT FK_Reviews_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId),
    CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5)
);
GO

CREATE TABLE dbo.AiConversations (
    ConversationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AiConversations PRIMARY KEY,
    CustomerId     INT NULL,
    Title          NVARCHAR(200) NULL,
    ConversationStatus VARCHAR(20) NOT NULL CONSTRAINT DF_AiConversations_Status DEFAULT 'active',
    CreatedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_AiConversations_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_AiConversations_UpdatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_AiConversations_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT CK_AiConversations_Status CHECK (ConversationStatus IN ('active','closed'))
);
GO

CREATE TABLE dbo.AiMessages (
    MessageId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AiMessages PRIMARY KEY,
    ConversationId     INT NOT NULL,
    SenderType         VARCHAR(20) NOT NULL,
    Content            NVARCHAR(MAX) NOT NULL,
    RecommendedFoodIds NVARCHAR(MAX) NULL,
    CreatedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_AiMessages_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_AiMessages_Conversations FOREIGN KEY (ConversationId) REFERENCES dbo.AiConversations(ConversationId),
    CONSTRAINT CK_AiMessages_Sender CHECK (SenderType IN ('customer','ai'))
);
GO

CREATE TABLE dbo.AuditLogs (
    AuditLogId    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    UserId        INT NULL,
    ActionName    VARCHAR(100) NOT NULL,
    EntityName    VARCHAR(100) NOT NULL,
    EntityId      VARCHAR(50) NULL,
    OldData       NVARCHAR(MAX) NULL,
    NewData       NVARCHAR(MAX) NULL,
    CreatedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO

/* =========================
   Useful indexes
   ========================= */
CREATE INDEX IX_Foods_Category_Status ON dbo.Foods(CategoryId, FoodStatus, IsActive);
CREATE INDEX IX_FoodVariants_Food_Active ON dbo.FoodVariants(FoodId, IsActive);
CREATE INDEX IX_Reservations_Date_Status ON dbo.Reservations(ReservationDate, ReservationStatus);
CREATE INDEX IX_Reservations_Customer_Date ON dbo.Reservations(CustomerId, ReservationDate DESC);
CREATE INDEX IX_Orders_Status_CreatedAt ON dbo.Orders(OrderStatus, CreatedAt DESC);
CREATE INDEX IX_OrderItems_Order_Status ON dbo.OrderItems(OrderId, ItemStatus);
CREATE INDEX IX_OrderTables_Table ON dbo.OrderTables(TableId, OrderId);
CREATE INDEX IX_Invoices_IssuedAt ON dbo.Invoices(IssuedAt DESC);
CREATE INDEX IX_InventoryItems_LowStock ON dbo.InventoryItems(CurrentQuantity, MinimumQuantity);
CREATE INDEX IX_StockMovements_Item_Date ON dbo.StockMovements(InventoryItemId, CreatedAt DESC);
CREATE INDEX IX_PointTransactions_Customer_Date ON dbo.PointTransactions(CustomerId, CreatedAt DESC);
CREATE INDEX IX_AiMessages_Conversation_Date ON dbo.AiMessages(ConversationId, CreatedAt);
CREATE INDEX IX_AuditLogs_User_Date ON dbo.AuditLogs(UserId, CreatedAt DESC);
GO

/* Initial roles. */
INSERT INTO dbo.Roles (RoleCode, RoleName, Description)
VALUES
    ('manager', N'Quản lý', N'Quản lý toàn bộ hệ thống nhà hàng'),
    ('staff', N'Nhân viên phục vụ', N'Quản lý bàn, order và thanh toán'),
    ('chef', N'Đầu bếp', N'Tiếp nhận và cập nhật trạng thái món ăn'),
    ('customer', N'Khách hàng', N'Đặt bàn, xem thực đơn và theo dõi lịch sử');
GO

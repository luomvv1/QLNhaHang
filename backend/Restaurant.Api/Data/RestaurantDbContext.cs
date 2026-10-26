using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Entities;

namespace Restaurant.Api.Data;

public partial class RestaurantDbContext : DbContext
{
    public RestaurantDbContext()
    {
    }

    public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AiConversation> AiConversations { get; set; }

    public virtual DbSet<AiMessage> AiMessages { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<DiningTable> DiningTables { get; set; }

    public virtual DbSet<Food> Foods { get; set; }

    public virtual DbSet<FoodCategory> FoodCategories { get; set; }

    public virtual DbSet<FoodIngredient> FoodIngredients { get; set; }

    public virtual DbSet<FoodVariant> FoodVariants { get; set; }

    public virtual DbSet<InventoryItem> InventoryItems { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderTable> OrderTables { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PointTransaction> PointTransactions { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionUsage> PromotionUsages { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<InventoryBatch> InventoryBatches { get; set; }

    public virtual DbSet<StockReceipt> StockReceipts { get; set; }

    public virtual DbSet<StockReceiptDetail> StockReceiptDetails { get; set; }

    public virtual DbSet<KitchenReconciliationLine> KitchenReconciliationLines { get; set; }

    public virtual DbSet<DailyPortionControl> DailyPortionControls { get; set; }

    public virtual DbSet<StockAudit> StockAudits { get; set; }

    public virtual DbSet<StockAuditItem> StockAuditItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost\\VANLUOM;Database=RestaurantManagement;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiConversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId);

            entity.Property(e => e.ConversationStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.AiConversations)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_AiConversations_Customers");
        });

        modelBuilder.Entity<AiMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId);

            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt }, "IX_AiMessages_Conversation_Date");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.SenderType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Conversation).WithMany(p => p.AiMessages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AiMessages_Conversations");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "IX_AuditLogs_User_Date").IsDescending(false, true);

            entity.Property(e => e.ActionName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EntityId)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EntityName)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.Phone, "UQ_Customers_Phone").IsUnique();

            entity.HasIndex(e => e.UserId, "UQ_Customers_User").IsUnique();

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TotalSpent).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.User).WithOne(p => p.Customer)
                .HasForeignKey<Customer>(d => d.UserId)
                .HasConstraintName("FK_Customers_Users");
        });

        modelBuilder.Entity<DiningTable>(entity =>
        {
            entity.HasKey(e => e.TableId);

            entity.HasIndex(e => e.TableCode, "UQ_DiningTables_Code").IsUnique();

            entity.Property(e => e.AreaName).HasMaxLength(100);
            entity.Property(e => e.Capacity).HasDefaultValue(4);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TableCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TableName).HasMaxLength(100);
            entity.Property(e => e.TableStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("empty");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<Food>(entity =>
        {
            entity.HasIndex(e => new { e.CategoryId, e.FoodStatus, e.IsActive }, "IX_Foods_Category_Status");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.FoodName).HasMaxLength(200);
            entity.Property(e => e.FoodStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("available");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MinPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Category).WithMany(p => p.Foods)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Foods_Categories");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.FoodCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Foods_CreatedBy");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.FoodUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_Foods_UpdatedBy");
        });

        modelBuilder.Entity<FoodCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.HasIndex(e => e.CategoryName, "UQ_FoodCategories_Name").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<FoodVariant>(entity =>
        {
            entity.Property(e => e.IsSoldOut).HasDefaultValue(false);
            entity.HasKey(e => e.VariantId);

            entity.HasIndex(e => new { e.FoodId, e.IsActive }, "IX_FoodVariants_Food_Active");

            entity.HasIndex(e => new { e.FoodId, e.VariantName }, "UQ_FoodVariants_Food_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Unit).HasMaxLength(30);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.VariantName).HasMaxLength(100);

            entity.HasOne(d => d.Food).WithMany(p => p.FoodVariants)
                .HasForeignKey(d => d.FoodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FoodVariants_Foods");
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasIndex(e => new { e.CurrentQuantity, e.MinimumQuantity }, "IX_InventoryItems_LowStock");

            entity.Property(e => e.CostPerUnit).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CurrentQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.PrepWastagePercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ItemName).HasMaxLength(200);
            entity.Property(e => e.MinimumQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Unit).HasMaxLength(30);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Supplier).WithMany(p => p.InventoryItems)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_InventoryItems_Suppliers");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(e => e.IssuedAt, "IX_Invoices_IssuedAt").IsDescending();

            entity.HasIndex(e => e.InvoiceCode, "UQ_Invoices_Code").IsUnique();

            entity.HasIndex(e => e.OrderId, "UQ_Invoices_Order").IsUnique();

            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("paid");
            entity.Property(e => e.IssuedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Invoices_Customers");

            entity.HasOne(d => d.IssuedByNavigation).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.IssuedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_IssuedBy");

            entity.HasOne(d => d.Order).WithOne(p => p.Invoice)
                .HasForeignKey<Invoice>(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Orders");
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.Property(e => e.FoodNameSnapshot).HasMaxLength(200);
            entity.Property(e => e.LineTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VariantSnapshot).HasMaxLength(100);

            entity.HasOne(d => d.Food).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.FoodId)
                .HasConstraintName("FK_InvoiceItems_Foods");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceItems_Invoices");

            entity.HasOne(d => d.Variant).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.VariantId)
                .HasConstraintName("FK_InvoiceItems_Variants");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.HasIndex(e => new { e.OrderStatus, e.CreatedAt }, "IX_Orders_Status_CreatedAt").IsDescending(false, true);

            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("open");
            entity.Property(e => e.PaidAt).HasPrecision(0);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_CreatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Orders_Customers");

            entity.HasOne(d => d.MergedIntoOrder).WithMany(p => p.InverseMergedIntoOrder)
                .HasForeignKey(d => d.MergedIntoOrderId)
                .HasConstraintName("FK_Orders_MergedInto");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.PortionDate).HasColumnType("date");
            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.CancelledFromStatus).HasMaxLength(20).IsUnicode(false);
            entity.HasIndex(e => new { e.OrderId, e.ItemStatus }, "IX_OrderItems_Order_Status");

            entity.Property(e => e.ConfirmedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FoodNameSnapshot).HasMaxLength(200);
            entity.Property(e => e.ItemStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending");
            entity.Property(e => e.LineTotal)
                .HasComputedColumnSql("([UnitPrice]*[Quantity])", true)
                .HasColumnType("decimal(29, 2)");
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.Property(e => e.ReadyAt).HasPrecision(0);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.VariantSnapshot).HasMaxLength(100);

            entity.HasOne(d => d.Chef).WithMany(p => p.OrderItemChefs)
                .HasForeignKey(d => d.ChefId)
                .HasConstraintName("FK_OrderItems_Chef");

            entity.HasOne(d => d.ConfirmedByNavigation).WithMany(p => p.OrderItemConfirmedByNavigations)
                .HasForeignKey(d => d.ConfirmedBy)
                .HasConstraintName("FK_OrderItems_ConfirmedBy");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.OrderItemCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_CreatedBy");

            entity.HasOne(d => d.Food).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.FoodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Foods");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Variant).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Variants");
        });

        modelBuilder.Entity<OrderTable>(entity =>
        {
            entity.HasKey(e => new { e.OrderId, e.TableId });

            entity.HasIndex(e => new { e.TableId, e.OrderId }, "IX_OrderTables_Table");

            entity.Property(e => e.AssignedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderTables_Orders");

            entity.HasOne(d => d.Table).WithMany(p => p.OrderTables)
                .HasForeignKey(d => d.TableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderTables_Tables");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaidAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("completed");
            entity.Property(e => e.TransactionCode)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Invoices");
        });

        modelBuilder.Entity<PointTransaction>(entity =>
        {
            entity.HasIndex(e => new { e.CustomerId, e.CreatedAt }, "IX_PointTransactions_Customer_Date").IsDescending(false, true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PointTransactions)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_PointTransactions_CreatedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.PointTransactions)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PointTransactions_Customers");

            entity.HasOne(d => d.Invoice).WithMany(p => p.PointTransactions)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_PointTransactions_Invoices");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasIndex(e => e.PromotionCode, "UQ_Promotions_Code").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EndAt).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MinimumOrderValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PromotionCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PromotionName).HasMaxLength(200);
            entity.Property(e => e.PromotionType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PromotionValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StartAt).HasPrecision(0);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Promotions_CreatedBy");
        });

        modelBuilder.Entity<PromotionUsage>(entity =>
        {
            entity.HasIndex(e => new { e.PromotionId, e.InvoiceId }, "UQ_PromotionUsages_Invoice").IsUnique();

            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UsedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.PromotionUsages)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_PromotionUsages_Customers");

            entity.HasOne(d => d.Invoice).WithMany(p => p.PromotionUsages)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromotionUsages_Invoices");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionUsages)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromotionUsages_Promotions");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasIndex(e => new { e.CustomerId, e.ReservationDate }, "IX_Reservations_Customer_Date").IsDescending(false, true);

            entity.HasIndex(e => new { e.ReservationDate, e.ReservationStatus }, "IX_Reservations_Date_Status");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerName).HasMaxLength(150);
            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.Property(e => e.ReservationDate).HasPrecision(0);
            entity.Property(e => e.ReservationStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.ConfirmedByNavigation).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.ConfirmedBy)
                .HasConstraintName("FK_Reservations_ConfirmedBy");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservations_Customers");

            entity.HasOne(d => d.Table).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.TableId)
                .HasConstraintName("FK_Reservations_Tables");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(e => e.Comment).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsVisible).HasDefaultValue(true);
            entity.Property(e => e.Reply).HasMaxLength(2000);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Customers");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_Reviews_Invoices");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleCode, "UQ_Roles_RoleCode").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RoleCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.RoleName).HasMaxLength(100);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasIndex(e => new { e.InventoryItemId, e.CreatedAt }, "IX_StockMovements_Item_Date").IsDescending(false, true);
            entity.HasIndex(e => new { e.BatchId, e.CreatedAt }, "IX_StockMovements_Batch_Date").IsDescending(false, true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MovementType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_CreatedBy");

            entity.HasOne(d => d.InventoryItem).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.InventoryItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_Inventory");

            entity.HasOne(d => d.Batch).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.BatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_Batch");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SupplierName).HasMaxLength(200);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLoginAt).HasPrecision(0);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<FoodIngredient>(entity =>
        {
            entity.HasKey(e => e.FoodIngredientId);
            entity.HasIndex(e => new { e.FoodId, e.VariantId, e.InventoryItemId }, "UQ_FoodIngredients_BOM").IsUnique();
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.QuantityPerUnit).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.WastagePercent).HasColumnType("decimal(5, 2)");
            entity.HasOne(d => d.Food).WithMany(p => p.FoodIngredients).HasForeignKey(d => d.FoodId);
            entity.HasOne(d => d.Variant).WithMany(p => p.FoodIngredients).HasForeignKey(d => d.VariantId);
            entity.HasOne(d => d.InventoryItem).WithMany(p => p.FoodIngredients).HasForeignKey(d => d.InventoryItemId);
        });

        modelBuilder.Entity<InventoryBatch>(entity =>
        {
            entity.HasKey(e => e.BatchId);
            entity.HasIndex(e => new { e.InventoryItemId, e.BatchCode }, "UQ_InventoryBatches_Item_Code").IsUnique();
            entity.HasIndex(e => new { e.InventoryItemId, e.ExpiryDate, e.ReceivedDate }, "IX_InventoryBatches_FEFO");
            entity.Property(e => e.BatchCode).HasMaxLength(50);
            entity.Property(e => e.QuantityRemaining).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.OriginalQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ExpiryDate).HasColumnType("date");
            entity.Property(e => e.ManufacturingDate).HasColumnType("date");
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false);
            entity.HasOne(d => d.InventoryItem).WithMany(p => p.InventoryBatches).HasForeignKey(d => d.InventoryItemId);
            entity.HasOne(d => d.Supplier).WithMany().HasForeignKey(d => d.SupplierId);
        });

        modelBuilder.Entity<StockReceipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId);
            entity.HasIndex(e => e.ReceiptCode, "UQ_StockReceipts_Code").IsUnique();
            entity.Property(e => e.ReceiptCode).HasMaxLength(50);
            entity.Property(e => e.ReceiptType).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UpdatedAt).HasPrecision(0).HasDefaultValueSql("(sysdatetime())");
            entity.HasOne(d => d.Supplier).WithMany().HasForeignKey(d => d.SupplierId);
            entity.HasOne(d => d.CreatedByNavigation).WithMany().HasForeignKey(d => d.CreatedBy).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.ApprovedByNavigation).WithMany().HasForeignKey(d => d.ApprovedBy);
        });

        modelBuilder.Entity<StockReceiptDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId);
            entity.Property(e => e.GrossQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.WastagePercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.WastageQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ExpectedUsableQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.AdjustmentNote).HasMaxLength(500);
            entity.Property(e => e.NewBatchCode).HasMaxLength(50);
            entity.Property(e => e.ManufacturingDate).HasColumnType("date");
            entity.Property(e => e.ExpiryDate).HasColumnType("date");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BomSnapshotJson).HasColumnType("nvarchar(max)");
            entity.HasOne(d => d.Receipt).WithMany(p => p.StockReceiptDetails).HasForeignKey(d => d.ReceiptId);
            entity.HasOne(d => d.InventoryItem).WithMany(p => p.StockReceiptDetails).HasForeignKey(d => d.InventoryItemId);
            entity.HasOne(d => d.Batch).WithMany(p => p.StockReceiptDetails).HasForeignKey(d => d.BatchId);
        });

        modelBuilder.Entity<KitchenReconciliationLine>(entity =>
        {
            entity.HasKey(e => e.ReconciliationLineId);
            entity.HasIndex(e => e.SourceDetailId, "UQ_KitchenReconciliationLines_SourceDetail").IsUnique();
            entity.Property(e => e.IssuedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UsedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ReturnedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.WastedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.WasteReason).HasMaxLength(500);
            entity.Property(e => e.ReconciledAt).HasPrecision(0);
            entity.HasOne(e => e.SourceDetail).WithMany().HasForeignKey(e => e.SourceDetailId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.ReturnReceipt).WithMany().HasForeignKey(e => e.ReturnReceiptId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.WasteReceipt).WithMany().HasForeignKey(e => e.WasteReceiptId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<DailyPortionControl>(entity =>
        {
            entity.HasKey(e => e.PortionControlId);
            entity.HasIndex(e => new { e.ControlDate, e.FoodId, e.VariantId }, "UQ_DailyPortionControls_Date_Item").IsUnique();
            entity.Property(e => e.ControlDate).HasColumnType("date");
            entity.Property(e => e.PlannedPortions).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CarryOverPortions).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CookedPortions).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoldPortions).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SystemRemain).HasColumnType("decimal(18, 2)").HasComputedColumnSql("([CookedPortions]-[SoldPortions])", stored: true);
            entity.Property(e => e.ActualRemain).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VariancePortions).HasColumnType("decimal(18, 2)").HasComputedColumnSql("(CASE WHEN [ActualRemain] IS NULL THEN NULL ELSE [ActualRemain]-([CookedPortions]-[SoldPortions]) END)", stored: true);
            entity.Property(e => e.DisposedPortions).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NextCarryOverPortions).HasColumnType("decimal(18, 2)");
            entity.HasOne(d => d.Food).WithMany(p => p.DailyPortionControls).HasForeignKey(d => d.FoodId);
            entity.HasOne(d => d.Variant).WithMany(p => p.DailyPortionControls).HasForeignKey(d => d.VariantId);
        });

        modelBuilder.Entity<StockAudit>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.HasIndex(e => e.AuditCode, "UQ_StockAudits_Code").IsUnique();
            entity.Property(e => e.AuditCode).HasMaxLength(50);
            entity.Property(e => e.AuditDate).HasColumnType("date");
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false);
            entity.HasOne(d => d.CreatedByNavigation).WithMany().HasForeignKey(d => d.CreatedBy).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.CompletedByNavigation).WithMany().HasForeignKey(d => d.CompletedBy);
        });

        modelBuilder.Entity<StockAuditItem>(entity =>
        {
            entity.HasKey(e => e.AuditItemId);
            entity.HasIndex(e => new { e.AuditId, e.InventoryItemId }, "UQ_StockAuditItems_Audit_Item").IsUnique();
            entity.Property(e => e.SystemQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ActualQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.DifferenceQuantity).HasColumnType("decimal(18, 4)");
            entity.HasOne(d => d.Audit).WithMany(p => p.StockAuditItems).HasForeignKey(d => d.AuditId);
            entity.HasOne(d => d.InventoryItem).WithMany(p => p.StockAuditItems).HasForeignKey(d => d.InventoryItemId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

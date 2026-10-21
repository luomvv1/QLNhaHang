using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class InventoryItem
{
    public int InventoryItemId { get; set; }

    public int? SupplierId { get; set; }

    public string ItemName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public string PurchaseUnit { get; set; } = "kg";

    public string BaseUnit { get; set; } = "g";

    public decimal ConversionRate { get; set; } = 1000m;

    public decimal PrepWastagePercent { get; set; }

    public decimal CurrentQuantity { get; set; } // Luong ton theo BaseUnit

    public decimal MinimumQuantity { get; set; } // Nguong canh bao theo BaseUnit

    public decimal CostPerUnit { get; set; } // Gia mua theo PurchaseUnit

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<FoodIngredient> FoodIngredients { get; set; } = new List<FoodIngredient>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>();

    public virtual ICollection<StockReceiptDetail> StockReceiptDetails { get; set; } = new List<StockReceiptDetail>();

    public virtual ICollection<StockAuditItem> StockAuditItems { get; set; } = new List<StockAuditItem>();

    public virtual Supplier? Supplier { get; set; }
}

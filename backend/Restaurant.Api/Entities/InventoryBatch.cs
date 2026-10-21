using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class InventoryBatch
{
    public int BatchId { get; set; }

    public int InventoryItemId { get; set; }

    public string BatchCode { get; set; } = null!;

    public decimal QuantityRemaining { get; set; } // Ton theo BaseUnit

    public decimal OriginalQuantity { get; set; } // So luong nhap theo BaseUnit

    public decimal UnitCost { get; set; } // Gia mua theo PurchaseUnit

    public DateTime? ManufacturingDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public DateTime ReceivedDate { get; set; }

    public int? SupplierId { get; set; }

    public string Status { get; set; } = "active"; // active, depleted, expired

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual InventoryItem InventoryItem { get; set; } = null!;

    public virtual Supplier? Supplier { get; set; }

    public virtual ICollection<StockReceiptDetail> StockReceiptDetails { get; set; } = new List<StockReceiptDetail>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

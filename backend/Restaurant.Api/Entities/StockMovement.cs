using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class StockMovement
{
    public int StockMovementId { get; set; }

    public int InventoryItemId { get; set; }

    public int? BatchId { get; set; }

    public string MovementType { get; set; } = null!;

    public decimal Quantity { get; set; } // So luong theo BaseUnit

    public decimal? UnitCost { get; set; } // Gia theo PurchaseUnit

    public string? Reason { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual InventoryItem InventoryItem { get; set; } = null!;

    public virtual InventoryBatch? Batch { get; set; }
}

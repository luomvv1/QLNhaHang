using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class StockAuditItem
{
    public int AuditItemId { get; set; }

    public int AuditId { get; set; }

    public int InventoryItemId { get; set; }

    public decimal SystemQuantity { get; set; }

    public decimal ActualQuantity { get; set; }

    public decimal DifferenceQuantity { get; set; }

    public string? Reason { get; set; }

    public virtual StockAudit Audit { get; set; } = null!;

    public virtual InventoryItem InventoryItem { get; set; } = null!;
}

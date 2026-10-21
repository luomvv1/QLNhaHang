using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class StockReceiptDetail
{
    public decimal? GrossQuantity { get; set; }

    public decimal WastagePercent { get; set; }

    public decimal WastageQuantity { get; set; }

    public decimal? ExpectedUsableQuantity { get; set; }

    public string? AdjustmentNote { get; set; }

    public int DetailId { get; set; }

    public int ReceiptId { get; set; }

    public int InventoryItemId { get; set; }

    public int? BatchId { get; set; }

    public decimal Quantity { get; set; } // Tính theo BaseUnit

    public decimal UnitCost { get; set; } // Gia theo PurchaseUnit

    public decimal Subtotal { get; set; }

    public string? Note { get; set; }

    // JSON snapshot of the BOM/portion context used to create a kitchen issue line.
    public string? BomSnapshotJson { get; set; }

    // Thông tin dùng khi approve phiếu import để tạo lô đúng theo dữ liệu nháp.
    public string? NewBatchCode { get; set; }

    public DateTime? ManufacturingDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public virtual StockReceipt Receipt { get; set; } = null!;

    public virtual InventoryItem InventoryItem { get; set; } = null!;

    public virtual InventoryBatch? Batch { get; set; }
}

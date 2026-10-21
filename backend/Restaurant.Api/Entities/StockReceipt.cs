using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class StockReceipt
{
    public int ReceiptId { get; set; }

    public string ReceiptCode { get; set; } = null!;

    public string ReceiptType { get; set; } = null!; // import_supplier, export_kitchen, return_from_kitchen, waste_disposal, audit_adjustment

    public string Status { get; set; } = "draft"; // draft, approved, cancelled

    public int? SupplierId { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    public int CreatedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual ICollection<StockReceiptDetail> StockReceiptDetails { get; set; } = new List<StockReceiptDetail>();
}

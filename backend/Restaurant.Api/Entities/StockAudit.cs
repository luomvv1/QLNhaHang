using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class StockAudit
{
    public int AuditId { get; set; }

    public string AuditCode { get; set; } = null!;

    public DateTime AuditDate { get; set; }

    public string Status { get; set; } = "draft"; // draft, completed, cancelled

    public string? Note { get; set; }

    public int CreatedBy { get; set; }

    public int? CompletedBy { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? CompletedByNavigation { get; set; }

    public virtual ICollection<StockAuditItem> StockAuditItems { get; set; } = new List<StockAuditItem>();
}

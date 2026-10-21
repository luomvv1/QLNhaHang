using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public int OrderId { get; set; }

    public int? CustomerId { get; set; }

    public int IssuedBy { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public int PointsUsed { get; set; }

    public int PointsEarned { get; set; }

    public decimal TotalAmount { get; set; }

    public string InvoiceStatus { get; set; } = null!;

    public DateTime IssuedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual User IssuedByNavigation { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();

    public virtual ICollection<PromotionUsage> PromotionUsages { get; set; } = new List<PromotionUsage>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}

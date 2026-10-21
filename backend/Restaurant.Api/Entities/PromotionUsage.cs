using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class PromotionUsage
{
    public int PromotionUsageId { get; set; }

    public int PromotionId { get; set; }

    public int InvoiceId { get; set; }

    public int? CustomerId { get; set; }

    public decimal DiscountAmount { get; set; }

    public DateTime UsedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Promotion Promotion { get; set; } = null!;
}

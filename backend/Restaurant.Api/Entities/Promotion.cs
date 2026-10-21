using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string PromotionCode { get; set; } = null!;

    public string PromotionName { get; set; } = null!;

    public string PromotionType { get; set; } = null!;

    public decimal PromotionValue { get; set; }

    public decimal MinimumOrderValue { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public bool IsActive { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<PromotionUsage> PromotionUsages { get; set; } = new List<PromotionUsage>();
}

using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int FoodId { get; set; }

    public int VariantId { get; set; }

    public string FoodNameSnapshot { get; set; } = null!;

    public string VariantSnapshot { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal? LineTotal { get; set; }

    public string? Note { get; set; }

    public string ItemStatus { get; set; } = null!;

    public int CreatedBy { get; set; }

    public int? ConfirmedBy { get; set; }

    public int? ChefId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? PortionDate { get; set; }

    public DateTime? ReadyAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public int? CancelledBy { get; set; }

    public string? CancellationReason { get; set; }

    public string? CancelledFromStatus { get; set; }

    public virtual User? Chef { get; set; }

    public virtual User? ConfirmedByNavigation { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Food Food { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual FoodVariant Variant { get; set; } = null!;
}

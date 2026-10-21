using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class InvoiceItem
{
    public int InvoiceItemId { get; set; }

    public int InvoiceId { get; set; }

    public int? FoodId { get; set; }

    public int? VariantId { get; set; }

    public string FoodNameSnapshot { get; set; } = null!;

    public string VariantSnapshot { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }

    public string? Note { get; set; }

    public virtual Food? Food { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual FoodVariant? Variant { get; set; }
}

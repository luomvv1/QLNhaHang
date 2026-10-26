using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class FoodVariant
{
    public int VariantId { get; set; }

    public int FoodId { get; set; }

    public string VariantName { get; set; } = null!;

    public decimal Price { get; set; }

    public string Unit { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public bool IsSoldOut { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Food Food { get; set; } = null!;

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<FoodIngredient> FoodIngredients { get; set; } = new List<FoodIngredient>();

    public virtual ICollection<DailyPortionControl> DailyPortionControls { get; set; } = new List<DailyPortionControl>();
}

using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class FoodIngredient
{
    public int FoodIngredientId { get; set; }

    public int FoodId { get; set; }

    public int? VariantId { get; set; }

    public int InventoryItemId { get; set; }

    public decimal QuantityPerUnit { get; set; }

    public decimal WastagePercent { get; set; } = 0m;

    public DateTime CreatedAt { get; set; }

    public virtual Food Food { get; set; } = null!;

    public virtual FoodVariant? Variant { get; set; }

    public virtual InventoryItem InventoryItem { get; set; } = null!;
}

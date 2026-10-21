using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class Food
{
    public int FoodId { get; set; }

    public int CategoryId { get; set; }

    public string FoodName { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string FoodStatus { get; set; } = null!;

    public decimal MinPrice { get; set; }

    public bool IsActive { get; set; }

    public int? CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual FoodCategory Category { get; set; } = null!;

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<FoodIngredient> FoodIngredients { get; set; } = new List<FoodIngredient>();

    public virtual ICollection<FoodVariant> FoodVariants { get; set; } = new List<FoodVariant>();

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<DailyPortionControl> DailyPortionControls { get; set; } = new List<DailyPortionControl>();

    public virtual User? UpdatedByNavigation { get; set; }
}

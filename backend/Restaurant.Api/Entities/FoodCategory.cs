using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class FoodCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Food> Foods { get; set; } = new List<Food>();
}

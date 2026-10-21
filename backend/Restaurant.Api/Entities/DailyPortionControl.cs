using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class DailyPortionControl
{
    public int PortionControlId { get; set; }

    public DateTime ControlDate { get; set; }

    public int FoodId { get; set; }

    public int? VariantId { get; set; }

    public decimal PlannedPortions { get; set; }

    public decimal CarryOverPortions { get; set; }

    public decimal CookedPortions { get; set; }

    public decimal SoldPortions { get; set; }

    public decimal SystemRemain { get; private set; }

    public decimal? ActualRemain { get; set; }

    public decimal? VariancePortions { get; private set; }

    public decimal DisposedPortions { get; set; }

    public decimal NextCarryOverPortions { get; set; }

    public string Status { get; set; } = "planned"; // planned, in_service, reconciled, closed

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Food Food { get; set; } = null!;

    public virtual FoodVariant? Variant { get; set; }
}

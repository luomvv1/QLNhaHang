using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class OrderTable
{
    public int OrderId { get; set; }

    public int TableId { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual DiningTable Table { get; set; } = null!;
}

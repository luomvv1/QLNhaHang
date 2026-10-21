using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class DiningTable
{
    public int TableId { get; set; }

    public string TableCode { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public string? AreaName { get; set; }

    public int Capacity { get; set; }

    public string TableStatus { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

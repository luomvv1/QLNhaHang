using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class Reservation
{
    public int ReservationId { get; set; }

    public int CustomerId { get; set; }

    public int? TableId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string CustomerPhone { get; set; } = null!;

    public DateTime ReservationDate { get; set; }

    public int GuestCount { get; set; }

    public string? Note { get; set; }

    public string ReservationStatus { get; set; } = null!;

    public int? ConfirmedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? ConfirmedByNavigation { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual DiningTable? Table { get; set; }
}

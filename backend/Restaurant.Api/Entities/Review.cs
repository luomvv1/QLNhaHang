using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class Review
{
    public int ReviewId { get; set; }

    public int CustomerId { get; set; }

    public int? InvoiceId { get; set; }

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public string? Reply { get; set; }

    public bool IsVisible { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Invoice? Invoice { get; set; }
}

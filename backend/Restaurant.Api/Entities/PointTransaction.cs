using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class PointTransaction
{
    public int PointTransactionId { get; set; }

    public int CustomerId { get; set; }

    public int? InvoiceId { get; set; }

    public string TransactionType { get; set; } = null!;

    public int Points { get; set; }

    public int BalanceAfter { get; set; }

    public string? Note { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Invoice? Invoice { get; set; }
}

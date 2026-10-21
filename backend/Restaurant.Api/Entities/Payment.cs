using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? TransactionCode { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public DateTime PaidAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}

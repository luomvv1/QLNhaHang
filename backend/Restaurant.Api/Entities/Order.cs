using System;
using System.Collections.Generic;

namespace Restaurant.Api.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public int? CustomerId { get; set; }

    public int CreatedBy { get; set; }

    public string OrderStatus { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    public int? MergedIntoOrderId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public int? CancelledBy { get; set; }

    public string? CancellationReason { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Order> InverseMergedIntoOrder { get; set; } = new List<Order>();

    public virtual Invoice? Invoice { get; set; }

    public virtual Order? MergedIntoOrder { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();
}

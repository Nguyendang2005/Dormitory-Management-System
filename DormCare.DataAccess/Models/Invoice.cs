using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public int StudentId { get; set; }

    public int RoomId { get; set; }

    public DateOnly BillingMonth { get; set; }

    public decimal RoomFee { get; set; }

    public decimal ServiceFee { get; set; }

    public decimal OtherFee { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateOnly DueDate { get; set; }

    public DateTime? PaidAt { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Room Room { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace DormCare.DataAccess.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public string PaymentCode { get; set; } = null!;

    public int InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? TransactionReference { get; set; }

    public DateTime PaymentDate { get; set; }

    public int? ReceivedBy { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual User? ReceivedByNavigation { get; set; }
}

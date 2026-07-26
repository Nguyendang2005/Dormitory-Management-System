using System;

namespace DormCare.Domain.Entities
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public string PaymentCode { get; set; } = string.Empty;

        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "BankTransfer"; // Cash, BankTransfer, MockPayment
        public string? TransactionReference { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public int? ReceivedBy { get; set; }
        public User? Receiver { get; set; }

        public string Status { get; set; } = "Completed"; // Pending, Completed, Failed, Refunded
        public string? Note { get; set; }
    }
}

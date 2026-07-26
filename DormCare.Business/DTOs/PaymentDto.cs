using System;

namespace DormCare.Business.DTOs
{
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public string PaymentCode { get; set; } = string.Empty;
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "BankTransfer";
        public string? TransactionReference { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string Status { get; set; } = "Completed";
        public string? Note { get; set; }
    }
}

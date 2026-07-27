using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormCare.Domain.Entities
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public DateTime BillingMonth { get; set; }
        public decimal RoomFee { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal OtherFee { get; set; }
        public decimal DiscountAmount { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal TotalAmount { get; set; } // Computed column

        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public string Status { get; set; } = "Unpaid"; // Draft, Unpaid, PartiallyPaid, Paid, Overdue, Cancelled
        public string? Note { get; set; }
        
        public bool IsDueReminderSent { get; set; } = false;
        public bool IsOverdueReminderSent { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}

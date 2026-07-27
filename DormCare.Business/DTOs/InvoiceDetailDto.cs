using System;
using System.Collections.Generic;

namespace DormCare.Business.DTOs
{
    public class InvoiceDetailDto
    {
        public int Id { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;

        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentPhone { get; set; } = string.Empty;

        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;

        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime BillingMonth { get; set; }

        public decimal RoomFee { get; set; }
        public decimal ElectricityFee { get; set; }
        public decimal WaterFee { get; set; }
        public decimal OtherFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainingBalance => Math.Max(0, TotalAmount - TotalPaid);

        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public string Status { get; set; } = "Unpaid";
        public string Note { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<PaymentDto> Payments { get; set; } = new();
    }
}

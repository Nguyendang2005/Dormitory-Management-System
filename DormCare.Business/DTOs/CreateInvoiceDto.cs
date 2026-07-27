using System;

namespace DormCare.Business.DTOs
{
    public class CreateInvoiceDto
    {
        public int StudentId { get; set; }
        public int RoomId { get; set; }
        public DateTime BillingMonth { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public decimal RoomFee { get; set; }
        public decimal ElectricityFee { get; set; }
        public decimal WaterFee { get; set; }
        public decimal OtherFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(10);
        public string? Note { get; set; }

        public decimal TotalAmount => RoomFee + ElectricityFee + WaterFee + OtherFee - DiscountAmount;
    }
}

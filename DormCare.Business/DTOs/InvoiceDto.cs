using System;

namespace DormCare.Business.DTOs
{
    public class InvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal RoomFee { get; set; }
        public decimal ElectricityFee { get; set; }
        public decimal WaterFee { get; set; }
        public decimal OtherFee { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "Unpaid";
        public string Note { get; set; } = string.Empty;
    }
}

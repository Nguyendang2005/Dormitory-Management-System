using System;

namespace DormCare.Business.DTOs
{
    public class BedDto
    {
        public int BedId { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;

        public string BedNumber { get; set; } = string.Empty;
        public string BedCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Occupied, Reserved, Maintenance
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public string Description { get; set; } = string.Empty;

        public string StatusVietnamese => Status switch
        {
            "Available"   => "Còn trống",
            "Occupied"    => "Đang sử dụng",
            "Reserved"    => "Đã đặt trước",
            "Maintenance" => "Bảo trì",
            _             => Status
        };

        public string StartDateDisplay => StartDate.HasValue ? StartDate.Value.ToString("dd/MM/yyyy") : "—";
        public string OccupantDisplay => string.IsNullOrEmpty(StudentName) ? "—" : $"{StudentName} ({StudentCode})";
    }
}

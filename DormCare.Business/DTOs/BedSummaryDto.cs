namespace DormCare.Business.DTOs
{
    public class BedSummaryDto
    {
        public int BedId { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string BedNumber { get; set; } = string.Empty;
        public string BedCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Occupied, Reserved, Maintenance
        public string Description { get; set; } = string.Empty;

        public string? OccupantStudentName { get; set; }
        public string? OccupantStudentCode { get; set; }

        public string StatusVietnamese => Status switch
        {
            "Available" => "Còn chỗ",
            "Occupied" => "Đang sử dụng",
            "Reserved" => "Đã đặt chỗ",
            "Maintenance" => "Bảo trì",
            _ => Status
        };
    }
}

namespace DormCare.Business.DTOs
{
    public class RoomAvailabilityDto
    {
        public int RoomId { get; set; }
        public int BuildingId { get; set; }
        public string BuildingName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public string RoomType { get; set; } = "Standard"; // Standard, Premium, Accessible
        public int Capacity { get; set; } = 6;
        public decimal MonthlyRent { get; set; }
        public string GenderType { get; set; } = "Male"; // Male, Female, Mixed
        public string Status { get; set; } = "Available"; // Available, Full, Maintenance, Inactive

        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public int ReservedBeds { get; set; }
        public int MaintenanceBeds { get; set; }
        public int TotalBedsCreated { get; set; }

        public string StatusVietnamese => Status switch
        {
            "Available" => "Còn chỗ",
            "Full" => "Đã đầy",
            "Maintenance" => "Bảo trì",
            "Inactive" => "Ngừng hoạt động",
            _ => Status
        };

        public string RoomTypeVietnamese => RoomType switch
        {
            "Standard" => "Tiêu chuẩn",
            "Premium" => "Cao cấp",
            "Accessible" => "Hỗ trợ",
            _ => RoomType
        };

        public string GenderTypeVietnamese => GenderType switch
        {
            "Male" => "Nam",
            "Female" => "Nữ",
            "Mixed" => "Hỗn hợp",
            _ => GenderType
        };

        public string OccupancyText => $"{OccupiedBeds} / {Capacity} đang sử dụng ({AvailableBeds} còn trống)";
        public double OccupancyPercentage => Capacity > 0 ? (double)OccupiedBeds / Capacity * 100 : 0;
        public double OccupancyRate => OccupancyPercentage;
    }
}

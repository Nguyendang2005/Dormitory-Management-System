namespace DormCare.Business.DTOs
{
    public class RoomDto
    {
        public int RoomId { get; set; }
        public int BuildingId { get; set; }
        public string BuildingCode { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public string RoomType { get; set; } = "Standard";
        public int Capacity { get; set; }
        public decimal MonthlyRent { get; set; }
        public string GenderType { get; set; } = "Male";
        public string Status { get; set; } = "Available";
        public string Description { get; set; } = string.Empty;

        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public int ReservedBeds { get; set; }
        public int MaintenanceBeds { get; set; }
        public int TotalBedsCreated { get; set; }
        public int TotalBeds
        {
            get => Capacity;
            set { }
        }

        public string OccupancySummary
        {
            get => $"{OccupiedBeds}/{Capacity} đang ở";
            set { }
        }

        public double OccupancyRate
        {
            get => Capacity > 0 ? (double)OccupiedBeds / Capacity * 100 : 0;
            set { }
        }

        public string StatusDisplay
        {
            get
            {
                if (Status == "Inactive") return "Vô hiệu";
                if (Status == "Maintenance") return "Bảo trì";
                if (AvailableBeds <= 0) return "Đã đầy";
                return "Còn chỗ";
            }
        }
    }
}

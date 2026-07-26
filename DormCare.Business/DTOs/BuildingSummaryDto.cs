namespace DormCare.Business.DTOs
{
    public class BuildingSummaryDto
    {
        public int BuildingId { get; set; }
        public string BuildingCode { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int NumberOfFloors { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance
        public string StatusVietnamese => Status switch
        {
            "Active" => "Còn hoạt động",
            "Inactive" => "Ngừng hoạt động",
            "Maintenance" => "Bảo trì",
            _ => Status
        };

        public int TotalRooms { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public int MaintenanceBeds { get; set; }
        public double OccupancyRate => TotalBeds > 0 ? (double)OccupiedBeds / TotalBeds * 100 : 0;
    }
}

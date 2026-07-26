namespace DormCare.Business.DTOs
{
    public class BuildingDto
    {
        public int BuildingId { get; set; }
        public string BuildingCode { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int NumberOfFloors { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        public int TotalRooms { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds => TotalBeds - OccupiedBeds;
        public double OccupancyRate => TotalBeds > 0 ? (double)OccupiedBeds / TotalBeds * 100 : 0;
    }
}

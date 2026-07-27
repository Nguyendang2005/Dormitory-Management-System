using System.Collections.Generic;

namespace DormCare.Business.DTOs
{
    public class BedDetailDto
    {
        public int BedId { get; set; }
        public string BedNumber { get; set; } = string.Empty;
        public string BedCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Occupied, Maintenance
        public string StudentName { get; set; } = "-";
        public string StudentCode { get; set; } = "-";
    }

    public class RoomDetailDto
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
        public int MaintenanceBeds { get; set; }

        public double OccupancyRate => Capacity > 0 ? (double)OccupiedBeds / Capacity * 100 : 0;
        public string OccupancyText => $"{OccupiedBeds}/{Capacity}";

        public List<BedDetailDto> Beds { get; set; } = new();
    }
}

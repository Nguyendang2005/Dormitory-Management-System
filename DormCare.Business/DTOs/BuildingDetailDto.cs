using System.Collections.Generic;

namespace DormCare.Business.DTOs
{
    public class BuildingDetailDto
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
        public int MaintenanceBeds { get; set; }

        public int AvailableBeds
        {
            get => TotalBeds - OccupiedBeds - MaintenanceBeds;
            set { }
        }

        public double OccupancyRate
        {
            get => TotalBeds > 0 ? (double)OccupiedBeds * 100.0 / TotalBeds : 0;
            set { }
        }

        public List<BuildingRoomSummaryDto> Rooms { get; set; } = new();
    }

    public class BuildingRoomSummaryDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public int Capacity { get; set; }
        public int OccupiedBeds { get; set; }

        public int AvailableBeds
        {
            get => Capacity - OccupiedBeds;
            set { }
        }

        public string Status { get; set; } = "Available"; // Available, Full, Maintenance, Inactive

        public string OccupancyText
        {
            get => $"{OccupiedBeds}/{Capacity}";
            set { }
        }

        public string StatusDisplay
        {
            get => OccupiedBeds >= Capacity ? "Đã đầy" : (Status == "Maintenance" ? "Bảo trì" : "Còn chỗ");
            set { }
        }
    }
}

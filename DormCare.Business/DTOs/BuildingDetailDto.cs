using System;
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
        public int TotalResidents { get; set; } // Số sinh viên đang ở (từ active RoomAssignment)

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

        public string OccupancyRateDisplay
        {
            get => $"{OccupancyRate:F1}%";
            set { }
        }

        public List<BuildingRoomSummaryDto> Rooms { get; set; } = new();
        public List<BuildingResidentDto> AllResidents { get; set; } = new();
    }

    public class BuildingRoomSummaryDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public int Capacity { get; set; }
        public int OccupiedBeds { get; set; }
        public int MaintenanceBeds { get; set; }

        public int AvailableBeds
        {
            get => Capacity - OccupiedBeds - MaintenanceBeds;
            set { }
        }

        public string Status { get; set; } = "Available";

        public string OccupancyText
        {
            get => $"{OccupiedBeds}/{Capacity}";
            set { }
        }

        public string StatusDisplay
        {
            get
            {
                if (Status == "Maintenance") return "Bảo trì";
                if (Status == "Inactive") return "Vô hiệu";
                if (OccupiedBeds >= Capacity) return "Đã đầy";
                if (AvailableBeds > 0) return "Còn chỗ";
                return "Còn chỗ";
            }
            set { }
        }

        // Cư trú hiện tại trong phòng này (từ active assignments)
        public List<BuildingResidentDto> CurrentResidents { get; set; } = new();
    }

    // Thông tin sinh viên đang cư trú trong tòa nhà
    public class BuildingResidentDto
    {
        public int AssignmentId { get; set; }
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string BedCode { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public DateTime StartDate { get; set; }
        public string AssignedByName { get; set; } = string.Empty;

        public string StartDateDisplay => StartDate.ToString("dd/MM/yyyy");
        public string FloorDisplay => $"Tầng {FloorNumber}";
    }
}

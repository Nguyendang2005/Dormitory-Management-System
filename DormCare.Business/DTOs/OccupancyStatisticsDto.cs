using System.Collections.Generic;

namespace DormCare.Business.DTOs
{
    public class OccupancyStatisticsDto
    {
        public int TotalBuildings { get; set; }
        public int TotalRooms { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public int ReservedBeds { get; set; }
        public int MaintenanceBeds { get; set; }

        public double OverallOccupancyRate => TotalBeds > 0 ? (double)OccupiedBeds / TotalBeds * 100 : 0;

        public List<BuildingSummaryDto> BuildingBreakdown { get; set; } = new();
    }
}

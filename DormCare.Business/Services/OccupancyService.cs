using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace DormCare.Business.Services
{
    public class OccupancyService
    {
        private readonly DormCareDbContext _dbContext;

        public OccupancyService(DormCareDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<OccupancyStatisticsDto> GetOccupancyStatisticsAsync()
        {
            var buildings = await _dbContext.Buildings
                .Include(b => b.Rooms)
                    .ThenInclude(r => r.Beds)
                .AsNoTracking()
                .ToListAsync();

            var stats = new OccupancyStatisticsDto
            {
                TotalBuildings = buildings.Count,
                TotalRooms = buildings.Sum(b => b.Rooms.Count),
                TotalBeds = buildings.Sum(b => b.Rooms.Sum(r => r.Beds.Count)),
                OccupiedBeds = buildings.Sum(b => b.Rooms.Sum(r => r.Beds.Count(bed => bed.Status == "Occupied"))),
                AvailableBeds = buildings.Sum(b => b.Rooms.Sum(r => r.Beds.Count(bed => bed.Status == "Available"))),
                ReservedBeds = buildings.Sum(b => b.Rooms.Sum(r => r.Beds.Count(bed => bed.Status == "Reserved"))),
                MaintenanceBeds = buildings.Sum(b => b.Rooms.Sum(r => r.Beds.Count(bed => bed.Status == "Maintenance")))
            };

            foreach (var b in buildings)
            {
                var bSummary = new BuildingSummaryDto
                {
                    BuildingId = b.BuildingId,
                    BuildingCode = b.BuildingCode,
                    BuildingName = b.BuildingName,
                    Address = b.Address,
                    NumberOfFloors = b.NumberOfFloors,
                    Description = b.Description ?? "",
                    Status = b.Status,
                    TotalRooms = b.Rooms.Count,
                    TotalBeds = b.Rooms.Sum(r => r.Beds.Count),
                    OccupiedBeds = b.Rooms.Sum(r => r.Beds.Count(bed => bed.Status == "Occupied")),
                    AvailableBeds = b.Rooms.Sum(r => r.Beds.Count(bed => bed.Status == "Available")),
                    MaintenanceBeds = b.Rooms.Sum(r => r.Beds.Count(bed => bed.Status == "Maintenance"))
                };
                stats.BuildingBreakdown.Add(bSummary);
            }

            return stats;
        }
    }
}

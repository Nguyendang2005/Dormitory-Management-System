using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.DataAccess.Repositories
{
    public class BuildingRepository : GenericRepository<Building>
    {
        public BuildingRepository(DormCareDbContext context) : base(context) { }

        /// <summary>Lấy danh sách tòa nhà kèm Rooms và Beds (cho tính toán thống kê).</summary>
        public async Task<IEnumerable<Building>> GetBuildingsWithRoomsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(b => b.Rooms)
                    .ThenInclude(r => r.Beds)
                .ToListAsync();
        }

        /// <summary>Lấy chi tiết một tòa nhà: Rooms → Beds → ActiveAssignments → Student.</summary>
        public async Task<Building?> GetBuildingDetailWithRoomsAsync(int buildingId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(b => b.Rooms)
                    .ThenInclude(r => r.Beds)
                        .ThenInclude(bed => bed.RoomAssignments
                            .Where(ra => ra.Status == "Active"))
                            .ThenInclude(ra => ra.Student)
                .Include(b => b.Rooms)
                    .ThenInclude(r => r.Beds)
                        .ThenInclude(bed => bed.RoomAssignments
                            .Where(ra => ra.Status == "Active"))
                            .ThenInclude(ra => ra.Manager)
                .FirstOrDefaultAsync(b => b.BuildingId == buildingId);
        }

        /// <summary>Lấy tất cả sinh viên đang cư trú trong một tòa nhà (active RoomAssignments).</summary>
        public async Task<IEnumerable<RoomAssignment>> GetBuildingActiveResidentsAsync(int buildingId)
        {
            var roomIds = await _context.Rooms
                .AsNoTracking()
                .Where(r => r.BuildingId == buildingId)
                .Select(r => r.RoomId)
                .ToListAsync();

            return await _context.RoomAssignments
                .AsNoTracking()
                .Include(ra => ra.Student)
                .Include(ra => ra.Bed)
                .Include(ra => ra.Room)
                .Include(ra => ra.Manager)
                .Where(ra => roomIds.Contains(ra.RoomId) && ra.Status == "Active")
                .OrderBy(ra => ra.Room.FloorNumber)
                    .ThenBy(ra => ra.Room.RoomNumber)
                    .ThenBy(ra => ra.Bed.BedCode)
                .ToListAsync();
        }

        public async Task<int> GetMaxFloorWithRoomsAsync(int buildingId)
        {
            var floors = await _context.Rooms
                .AsNoTracking()
                .Where(r => r.BuildingId == buildingId)
                .Select(r => r.FloorNumber)
                .ToListAsync();

            return floors.Any() ? floors.Max() : 0;
        }

        public async Task<(int TotalRooms, int TotalBeds, int ResidingStudents, int MaxFloorWithRooms)> GetBuildingUsageAsync(int buildingId)
        {
            var rooms = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.Beds)
                .Where(r => r.BuildingId == buildingId)
                .ToListAsync();

            int totalRooms = rooms.Count;
            int maxFloor = rooms.Any() ? rooms.Max(r => r.FloorNumber) : 0;
            int totalBeds = rooms.Sum(r => r.Beds.Count > 0 ? r.Beds.Count : r.Capacity);

            var roomIds = rooms.Select(r => r.RoomId).ToList();

            int residingStudents = await _context.RoomAssignments
                .AsNoTracking()
                .Where(ra => roomIds.Contains(ra.RoomId) && ra.Status == "Active")
                .CountAsync();

            return (totalRooms, totalBeds, residingStudents, maxFloor);
        }
    }
}

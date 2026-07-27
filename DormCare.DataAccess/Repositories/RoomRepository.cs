using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.DataAccess.Repositories
{
    public class RoomRepository : GenericRepository<Room>
    {
        public RoomRepository(DormCareDbContext context) : base(context) { }

        public DormCareDbContext DbContext => _context;

        public async Task<IEnumerable<Room>> GetRoomsWithBuildingAndBedsAsync()
        {
            return await _dbSet
                .Include(r => r.Building)
                .Include(r => r.Beds)
                    .ThenInclude(b => b.RoomAssignments.Where(ra => ra.Status == "Active"))
                        .ThenInclude(ra => ra.Student)
                .Include(r => r.RoomAssignments.Where(ra => ra.Status == "Active"))
                    .ThenInclude(ra => ra.Student)
                .ToListAsync();
        }

        public async Task<Room?> GetRoomWithDetailsAsync(int roomId)
        {
            return await _dbSet
                .Include(r => r.Building)
                .Include(r => r.Beds)
                    .ThenInclude(b => b.RoomAssignments.Where(ra => ra.Status == "Active"))
                        .ThenInclude(ra => ra.Student)
                .Include(r => r.RoomAssignments.Where(ra => ra.Status == "Active"))
                    .ThenInclude(ra => ra.Student)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);
        }

        public async Task<IEnumerable<RoomAssignment>> GetRoomAllAssignmentsAsync(int roomId)
        {
            return await _context.RoomAssignments
                .AsNoTracking()
                .Include(ra => ra.Student)
                .Include(ra => ra.Bed)
                .Include(ra => ra.Manager)
                .Where(ra => ra.RoomId == roomId)
                .OrderByDescending(ra => ra.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(int? buildingId = null, string? genderType = null, string? roomType = null)
        {
            var query = _dbSet
                .Include(r => r.Building)
                .Include(r => r.Beds)
                .Where(r =>
                    r.Building != null &&
                    r.Building.Status == "Active" &&
                    r.Status == "Available" &&
                    r.Beds.Any(b => b.Status == "Available"));

            if (buildingId.HasValue)
            {
                query = query.Where(r => r.BuildingId == buildingId.Value);
            }

            if (!string.IsNullOrWhiteSpace(genderType))
            {
                query = query.Where(r => r.GenderType == genderType);
            }

            if (!string.IsNullOrWhiteSpace(roomType))
            {
                query = query.Where(r => r.RoomType == roomType);
            }

            return await query.ToListAsync();
        }
    }
}

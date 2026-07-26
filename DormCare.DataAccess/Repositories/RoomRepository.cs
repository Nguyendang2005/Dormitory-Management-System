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

        public async Task<IEnumerable<Room>> GetRoomsWithBuildingAndBedsAsync()
        {
            return await _dbSet
                .Include(r => r.Building)
                .Include(r => r.Beds)
                .ToListAsync();
        }

        public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(int? buildingId = null, string? genderType = null, string? roomType = null)
        {
            var query = _dbSet
                .Include(r => r.Building)
                .Include(r => r.Beds)
                .Where(r => r.Status == "Available" && r.Beds.Any(b => b.Status == "Available"));

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

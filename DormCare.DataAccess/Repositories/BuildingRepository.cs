using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.DataAccess.Repositories
{
    public class BuildingRepository : GenericRepository<Building>
    {
        public BuildingRepository(DormCareDbContext context) : base(context) { }

        public async Task<IEnumerable<Building>> GetBuildingsWithRoomsAsync()
        {
            return await _dbSet
                .Include(b => b.Rooms)
                    .ThenInclude(r => r.Beds)
                .ToListAsync();
        }
    }
}

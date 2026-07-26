using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.DataAccess.Repositories
{
    public class BedRepository : GenericRepository<Bed>
    {
        public BedRepository(DormCareDbContext context) : base(context) { }

        public async Task<IEnumerable<Bed>> GetBedsByRoomIdAsync(int roomId)
        {
            return await _dbSet
                .Include(b => b.Room)
                    .ThenInclude(r => r.Building)
                .Where(b => b.RoomId == roomId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Bed>> GetAvailableBedsAsync()
        {
            return await _dbSet
                .Include(b => b.Room)
                    .ThenInclude(r => r.Building)
                .Where(b => b.Status == "Available")
                .ToListAsync();
        }
    }
}

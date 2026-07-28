using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.DataAccess.Repositories
{
    public class StudentRepository : GenericRepository<Student>
    {
        public StudentRepository(DormCareDbContext context) : base(context) { }

        public async Task<IEnumerable<Student>> GetStudentsWithDetailsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.RoomAssignments)
                    .ThenInclude(ra => ra.Room)
                        .ThenInclude(r => r.Building)
                .Include(s => s.RoomAssignments)
                    .ThenInclude(ra => ra.Bed)
                .ToListAsync();
        }

        public async Task<Student?> GetStudentByUserIdAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.RoomAssignments)
                    .ThenInclude(ra => ra.Room)
                        .ThenInclude(r => r.Building)
                .Include(s => s.RoomAssignments)
                    .ThenInclude(ra => ra.Bed)
                .Include(s => s.Invoices)
                .Include(s => s.MaintenanceRequests)
                .Include(s => s.RoomApplications)
                    .ThenInclude(a => a.Room)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }
    }
}

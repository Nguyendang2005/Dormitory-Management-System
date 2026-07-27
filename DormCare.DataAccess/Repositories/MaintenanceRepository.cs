using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.DataAccess.Repositories
{
    public class MaintenanceRepository : GenericRepository<MaintenanceRequest>
    {
        public MaintenanceRepository(DormCareDbContext context) : base(context) { }

        public async Task<IEnumerable<MaintenanceRequest>> GetRequestsWithDetailsAsync()
        {
            return await _dbSet
                .Include(m => m.Student)
                    .ThenInclude(s => s.User)
                .Include(m => m.Room)
                    .ThenInclude(r => r.Building)
                .OrderBy(m => m.Status == "Closed" || m.Status == "Resolved" ? 1 : 0)
                .ThenBy(m => m.Priority == "Urgent" ? 1 :
                             m.Priority == "High" ? 2 :
                             m.Priority == "Medium" ? 3 : 4)
                .ThenBy(m => m.CreatedAt) // FIFO: Đơn gửi từ lâu chưa xử lý được ưu tiên nổi lên trước
                .ToListAsync();
        }

        public async Task<IEnumerable<MaintenanceRequest>> GetRequestsByStudentIdAsync(int studentId)
        {
            return await _dbSet
                .Include(m => m.Room)
                    .ThenInclude(r => r.Building)
                .Where(m => m.StudentId == studentId)
                .OrderBy(m => m.Status == "Closed" || m.Status == "Resolved" ? 1 : 0)
                .ThenBy(m => m.Priority == "Urgent" ? 1 :
                             m.Priority == "High" ? 2 :
                             m.Priority == "Medium" ? 3 : 4)
                .ThenBy(m => m.CreatedAt) // FIFO: Đơn gửi từ lâu chưa xử lý được ưu tiên nổi lên trước
                .ToListAsync();
        }

        public async Task<MaintenanceRequest?> GetRequestByIdWithDetailsAsync(int requestId)
        {
            return await _dbSet
                .Include(m => m.Student)
                .FirstOrDefaultAsync(m => m.RequestId == requestId);
        }
    }
}

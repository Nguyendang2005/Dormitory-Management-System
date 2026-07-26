using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.DataAccess.Repositories
{
    public class InvoiceRepository : GenericRepository<Invoice>
    {
        public InvoiceRepository(DormCareDbContext context) : base(context) { }

        public async Task<IEnumerable<Invoice>> GetInvoicesWithStudentAsync()
        {
            return await _dbSet
                .Include(i => i.Student)
                    .ThenInclude(s => s.User)
                .Include(i => i.Room)
                    .ThenInclude(r => r.Building)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByStudentIdAsync(int studentId)
        {
            return await _dbSet
                .Include(i => i.Student)
                    .ThenInclude(s => s.User)
                .Include(i => i.Room)
                .Where(i => i.StudentId == studentId)
                .ToListAsync();
        }
    }
}

using System;
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
                .Include(i => i.Payments)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByStudentIdAsync(int studentId)
        {
            return await _dbSet
                .Include(i => i.Student)
                    .ThenInclude(s => s.User)
                .Include(i => i.Room)
                    .ThenInclude(r => r.Building)
                .Include(i => i.Payments)
                .Where(i => i.StudentId == studentId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<Invoice?> GetByIdWithDetailsAsync(int invoiceId)
        {
            return await _dbSet
                .Include(i => i.Student)
                    .ThenInclude(s => s.User)
                .Include(i => i.Room)
                    .ThenInclude(r => r.Building)
                .Include(i => i.Payments)
                    .ThenInclude(p => p.Receiver)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task<IEnumerable<Invoice>> GetUnpaidInvoicesAsync()
        {
            return await _dbSet
                .Include(i => i.Student)
                    .ThenInclude(s => s.User)
                .Include(i => i.Room)
                    .ThenInclude(r => r.Building)
                .Include(i => i.Payments)
                .Where(i => i.Status == "Unpaid" || i.Status == "Overdue" || i.Status == "PartiallyPaid")
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<string> GenerateNextInvoiceCodeAsync()
        {
            string prefix = $"INV-{DateTime.UtcNow:yyyyMM}-";
            var latestCode = await _dbSet
                .Where(i => i.InvoiceCode.StartsWith(prefix))
                .Select(i => i.InvoiceCode)
                .OrderByDescending(c => c)
                .FirstOrDefaultAsync();

            int nextSequence = 1;
            if (!string.IsNullOrEmpty(latestCode) && latestCode.Length >= prefix.Length + 4)
            {
                if (int.TryParse(latestCode.Substring(prefix.Length), out int seq))
                {
                    nextSequence = seq + 1;
                }
            }

            return $"{prefix}{nextSequence:D4}";
        }
    }
}

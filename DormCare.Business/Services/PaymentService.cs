using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class PaymentService
    {
        private readonly DormCareDbContext _context;

        public PaymentService(DormCareDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetStudentTotalDebtAsync(int studentId)
        {
            var studentInvoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.StudentId == studentId)
                .ToListAsync();

            decimal totalDebt = 0;
            foreach (var inv in studentInvoices)
            {
                decimal paid = inv.Payments
                    .Where(p => p.Status == "Completed")
                    .Sum(p => p.Amount);

                decimal rem = Math.Max(0, inv.TotalAmount - paid);
                totalDebt += rem;
            }

            return totalDebt;
        }

        public async Task<ServiceResult<bool>> ProcessPaymentAsync(int invoiceId, decimal amount, string method, string transactionRef, int? receivedBy = null, string? note = null)
        {
            if (amount <= 0)
                return ServiceResult<bool>.Failure("Số tiền thanh toán phải lớn hơn 0.");

            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                return ServiceResult<bool>.Failure("Hóa đơn không tồn tại.");

            var allStudentInvoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.StudentId == invoice.StudentId)
                .ToListAsync();

            var unpaidList = allStudentInvoices
                .Select(inv => new
                {
                    Invoice = inv,
                    AlreadyPaid = inv.Payments.Where(p => p.Status == "Completed").Sum(p => p.Amount),
                    Remaining = Math.Max(0, inv.TotalAmount - inv.Payments.Where(p => p.Status == "Completed").Sum(p => p.Amount))
                })
                .Where(x => x.Remaining > 0)
                .OrderBy(x => x.Invoice.InvoiceId == invoiceId ? 0 : 1)
                .ThenBy(x => x.Invoice.DueDate)
                .ToList();

            if (!unpaidList.Any())
                return ServiceResult<bool>.Failure("Tất cả các hóa đơn của sinh viên này đã được thanh toán đủ.");

            decimal totalStudentDebt = unpaidList.Sum(x => x.Remaining);
            if (amount > totalStudentDebt)
            {
                return ServiceResult<bool>.Failure($"Số tiền thanh toán ({amount:N0} VNĐ) vượt quá tổng dư nợ ({totalStudentDebt:N0} VNĐ) của sinh viên.");
            }

            decimal pool = amount;
            int clearedInvoices = 0;

            foreach (var item in unpaidList)
            {
                if (pool <= 0) break;

                var targetInv = item.Invoice;
                decimal payAmount = Math.Min(pool, item.Remaining);

                var payment = new Payment
                {
                    PaymentCode = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                    InvoiceId = targetInv.InvoiceId,
                    Amount = payAmount,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = string.IsNullOrWhiteSpace(method) ? "BankTransfer" : method,
                    TransactionReference = transactionRef,
                    ReceivedBy = receivedBy,
                    Status = "Completed",
                    Note = string.IsNullOrWhiteSpace(note) ? $"Thanh toán cho hóa đơn {targetInv.InvoiceCode}" : note
                };

                _context.Payments.Add(payment);

                decimal newTotalPaid = item.AlreadyPaid + payAmount;
                if (newTotalPaid >= targetInv.TotalAmount)
                {
                    targetInv.Status = "Paid";
                    targetInv.PaidAt = DateTime.UtcNow;
                    clearedInvoices++;
                }
                else
                {
                    targetInv.Status = "PartiallyPaid";
                }

                pool -= payAmount;
            }

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Success(true, $"Thanh toán thành công {amount:N0} VNĐ ({clearedInvoices} hóa đơn hoàn tất thanh toán).");
        }
    }
}

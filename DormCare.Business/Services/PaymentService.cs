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
                .Where(i => i.StudentId == studentId && i.Status != "Cancelled")
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

        public async Task<ServiceResult<bool>> ProcessPaymentAsync(int invoiceId, decimal amount, string method, string transactionRef, int? receivedBy = null, string? note = null, string paymentStatus = "Completed")
        {
            if (amount <= 0)
                return ServiceResult<bool>.Failure("Số tiền thanh toán phải lớn hơn 0 (BR-PAY-01).");

            var targetInvoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (targetInvoice == null)
                return ServiceResult<bool>.Failure("Hóa đơn không tồn tại.");

            // BR-PAY-04: Không được thanh toán cho Hóa đơn đã bị Hủy
            if (targetInvoice.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<bool>.Failure("Không thể thực hiện thanh toán cho hóa đơn đã bị hủy (BR-PAY-04).");
            }

            // Lấy tất cả các hóa đơn chưa thanh toán đủ của sinh viên (bỏ qua Cancelled)
            var allStudentInvoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.StudentId == targetInvoice.StudentId && i.Status != "Cancelled")
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
            {
                return ServiceResult<bool>.Failure("Tất cả các hóa đơn của sinh viên này đã được thanh toán hoàn tất (BR-PAY-06).");
            }

            decimal totalStudentDebt = unpaidList.Sum(x => x.Remaining);

            // BR-PAY-03: Số tiền thanh toán không được lớn hơn TỔNG DƯ NỢ của sinh viên
            if (amount > totalStudentDebt)
            {
                return ServiceResult<bool>.Failure($"Số tiền thanh toán ({amount:N0} VNĐ) không được vượt quá tổng dư nợ của sinh viên ({totalStudentDebt:N0} VNĐ) (BR-PAY-03).");
            }

            // BR-PAY-06: Kiểm tra trùng mã giao dịch (Single completion check)
            if (!string.IsNullOrWhiteSpace(transactionRef) &&
                await _context.Payments.AnyAsync(p => p.TransactionReference == transactionRef && p.Status == "Completed"))
            {
                return ServiceResult<bool>.Failure($"Mã giao dịch '{transactionRef}' đã được xử lý và xác nhận Completed trước đó. Tránh thanh toán trùng (BR-PAY-06).");
            }

            // BR-PAY-05: Giao dịch Failed không cập nhật Hóa đơn
            if (paymentStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                var failedPayment = new Payment
                {
                    PaymentCode = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                    InvoiceId = targetInvoice.InvoiceId,
                    Amount = amount,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = string.IsNullOrWhiteSpace(method) ? "BankTransfer" : method,
                    TransactionReference = transactionRef,
                    ReceivedBy = receivedBy,
                    Status = "Failed",
                    Note = string.IsNullOrWhiteSpace(note) ? "Giao dịch thanh toán thất bại" : note
                };

                _context.Payments.Add(failedPayment);
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Failure("Giao dịch thanh toán thất bại (Failed). Hóa đơn chưa được cập nhật (BR-PAY-05).");
            }

            // Xử lý phân bổ tiền thanh toán vào hóa đơn đang chọn và các hóa đơn nợ khác
            decimal pool = amount;
            int clearedInvoices = 0;

            foreach (var item in unpaidList)
            {
                if (pool <= 0) break;

                var inv = item.Invoice;
                decimal payAmount = Math.Min(pool, item.Remaining);

                var payment = new Payment
                {
                    PaymentCode = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                    InvoiceId = inv.InvoiceId,
                    Amount = payAmount,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = string.IsNullOrWhiteSpace(method) ? "BankTransfer" : method,
                    TransactionReference = transactionRef,
                    ReceivedBy = receivedBy,
                    Status = "Completed",
                    Note = string.IsNullOrWhiteSpace(note) ? $"Thanh toán cho hóa đơn {inv.InvoiceCode}" : note
                };

                _context.Payments.Add(payment);

                decimal newTotalPaid = item.AlreadyPaid + payAmount;
                if (newTotalPaid >= inv.TotalAmount)
                {
                    inv.Status = "Paid";
                    inv.PaidAt = DateTime.UtcNow;
                    clearedInvoices++;
                }
                else
                {
                    inv.Status = "PartiallyPaid";
                }

                pool -= payAmount;
            }

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Success(true, $"Thanh toán thành công {amount:N0} VNĐ ({clearedInvoices} hóa đơn hoàn tất thanh toán).");
        }
    }
}

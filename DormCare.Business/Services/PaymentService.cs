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

        public async Task<ServiceResult<bool>> ProcessPaymentAsync(int invoiceId, decimal amount, string method, string transactionRef, int? receivedBy = null, string? note = null)
        {
            if (amount <= 0)
                return ServiceResult<bool>.Failure("Số tiền thanh toán phải lớn hơn 0.");

            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                return ServiceResult<bool>.Failure("Hóa đơn không tồn tại.");

            decimal alreadyPaid = invoice.Payments
                .Where(p => p.Status == "Completed")
                .Sum(p => p.Amount);

            decimal remaining = Math.Max(0, invoice.TotalAmount - alreadyPaid);

            if (remaining <= 0)
                return ServiceResult<bool>.Failure("Hóa đơn này đã được thanh toán đủ.");

            var payment = new Payment
            {
                PaymentCode = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                InvoiceId = invoiceId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = string.IsNullOrWhiteSpace(method) ? "BankTransfer" : method,
                TransactionReference = transactionRef,
                ReceivedBy = receivedBy,
                Status = "Completed",
                Note = string.IsNullOrWhiteSpace(note) ? $"Thanh toán cho hóa đơn {invoice.InvoiceCode}" : note
            };

            _context.Payments.Add(payment);

            decimal newTotalPaid = alreadyPaid + amount;
            if (newTotalPaid >= invoice.TotalAmount)
            {
                invoice.Status = "Paid";
                invoice.PaidAt = DateTime.UtcNow;
            }
            else
            {
                invoice.Status = "PartiallyPaid";
            }

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Success(true, $"Cập nhật thanh toán thành công! Đã thu: {amount:N0} VNĐ.");
        }
    }
}

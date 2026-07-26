using System;
using System.Threading.Tasks;
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

        public async Task<ServiceResult<bool>> ProcessPaymentAsync(int invoiceId, decimal amount, string method, string transactionRef)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return ServiceResult<bool>.Failure("Hóa đơn không tồn tại.");

            var payment = new Payment
            {
                PaymentCode = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{invoiceId}",
                InvoiceId = invoiceId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = method,
                TransactionReference = transactionRef,
                Status = "Completed",
                Note = $"Thanh toán cho hóa đơn {invoice.InvoiceCode}"
            };

            _context.Payments.Add(payment);
            invoice.Status = "Paid";
            invoice.PaidAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Success(true, "Thanh toán thành công!");
        }
    }
}

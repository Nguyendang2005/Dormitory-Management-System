using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Repositories;

namespace DormCare.Business.Services
{
    public class InvoiceService
    {
        private readonly InvoiceRepository _invoiceRepository;

        public InvoiceService(InvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync()
        {
            var invoices = await _invoiceRepository.GetInvoicesWithStudentAsync();
            return invoices.Select(i => new InvoiceDto
            {
                Id = i.InvoiceId,
                InvoiceCode = i.InvoiceCode,
                StudentName = i.Student?.FullName ?? "Unknown",
                StudentCode = i.Student?.StudentCode ?? "N/A",
                Month = i.BillingMonth.Month,
                Year = i.BillingMonth.Year,
                RoomFee = i.RoomFee,
                ElectricityFee = i.ServiceFee,
                WaterFee = i.OtherFee,
                OtherFee = 0,
                TotalAmount = i.TotalAmount,
                DueDate = i.DueDate,
                Status = i.Status,
                Note = i.Note ?? string.Empty
            });
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByStudentIdAsync(int studentId)
        {
            var invoices = await _invoiceRepository.GetInvoicesByStudentIdAsync(studentId);
            return invoices.Select(i => new InvoiceDto
            {
                Id = i.InvoiceId,
                InvoiceCode = i.InvoiceCode,
                StudentName = i.Student?.FullName ?? "Unknown",
                StudentCode = i.Student?.StudentCode ?? "N/A",
                Month = i.BillingMonth.Month,
                Year = i.BillingMonth.Year,
                RoomFee = i.RoomFee,
                ElectricityFee = i.ServiceFee,
                WaterFee = i.OtherFee,
                OtherFee = 0,
                TotalAmount = i.TotalAmount,
                DueDate = i.DueDate,
                Status = i.Status,
                Note = i.Note ?? string.Empty
            });
        }

        public async Task<ServiceResult<bool>> MarkAsPaidAsync(int invoiceId)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
            if (invoice == null) return ServiceResult<bool>.Failure("Không tìm thấy hóa đơn.");

            invoice.Status = "Paid";
            invoice.PaidAt = DateTime.UtcNow;

            await _invoiceRepository.UpdateAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return ServiceResult<bool>.Success(true, "Xác nhận thanh toán hóa đơn thành công!");
        }
    }
}

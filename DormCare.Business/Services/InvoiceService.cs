using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class InvoiceService
    {
        private readonly InvoiceRepository _invoiceRepository;
        private readonly StudentRepository _studentRepository;
        private readonly RoomRepository _roomRepository;
        private readonly NotificationService _notificationService;

        public InvoiceService(
            InvoiceRepository invoiceRepository,
            StudentRepository studentRepository,
            RoomRepository roomRepository,
            NotificationService notificationService)
        {
            _invoiceRepository = invoiceRepository;
            _studentRepository = studentRepository;
            _roomRepository = roomRepository;
            _notificationService = notificationService;
        }

        public decimal CalculateTotalFee(decimal roomFee, decimal serviceFee, decimal waterFee, decimal discountAmount)
        {
            return Math.Max(0, roomFee + serviceFee + waterFee - discountAmount);
        }

        public async Task<ServiceResult<InvoiceDto>> CreateInvoiceAsync(CreateInvoiceDto dto)
        {
            var student = await _studentRepository.GetByIdAsync(dto.StudentId);
            if (student == null)
                return ServiceResult<InvoiceDto>.Failure("Không tìm thấy sinh viên đã chọn.");

            var room = await _roomRepository.GetByIdAsync(dto.RoomId);
            if (room == null)
                return ServiceResult<InvoiceDto>.Failure("Không tìm thấy phòng đã chọn.");

            DateTime billingMonth = new DateTime(dto.BillingMonth.Year, dto.BillingMonth.Month, 1);

            var existingInvoices = await _invoiceRepository.FindAsync(
                i => i.StudentId == dto.StudentId &&
                     i.BillingMonth.Month == dto.BillingMonth.Month &&
                     i.BillingMonth.Year == dto.BillingMonth.Year);
            if (existingInvoices.Any())
            {
                return ServiceResult<InvoiceDto>.Failure($"Sinh viên {student.FullName} đã có hóa đơn cho tháng {dto.BillingMonth:MM/yyyy} rồi!");
            }

            string invoiceCode = await _invoiceRepository.GenerateNextInvoiceCodeAsync();
            decimal totalCalculated = CalculateTotalFee(dto.RoomFee, dto.ElectricityFee, dto.WaterFee + dto.OtherFee, dto.DiscountAmount);

            var invoice = new Invoice
            {
                InvoiceCode = invoiceCode,
                StudentId = dto.StudentId,
                RoomId = dto.RoomId,
                BillingMonth = billingMonth,
                RoomFee = dto.RoomFee,
                ServiceFee = dto.ElectricityFee,
                OtherFee = dto.WaterFee + dto.OtherFee,
                DiscountAmount = dto.DiscountAmount,
                TotalAmount = totalCalculated,
                DueDate = dto.DueDate,
                Status = "Unpaid",
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _invoiceRepository.AddAsync(invoice);
                await _invoiceRepository.SaveChangesAsync();
            }
            catch (Exception)
            {
                return ServiceResult<InvoiceDto>.Failure($"Lỗi lưu hóa đơn: Hóa đơn tháng {billingMonth:MM/yyyy} của sinh viên này đã tồn tại.");
            }

            var createdInvoice = await _invoiceRepository.GetByIdWithDetailsAsync(invoice.InvoiceId);
            return ServiceResult<InvoiceDto>.Success(MapToDto(createdInvoice!), "Tạo hóa đơn thành công!");
        }

        public async Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync()
        {
            var invoices = (await _invoiceRepository.GetInvoicesWithStudentAsync()).ToList();
            await SyncInvoiceStatusesAsync(invoices);
            return invoices.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetUnpaidInvoicesAsync()
        {
            var invoices = (await _invoiceRepository.GetUnpaidInvoicesAsync()).ToList();
            await SyncInvoiceStatusesAsync(invoices);
            return invoices.Select(MapToDto);
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByStudentIdAsync(int studentId)
        {
            var invoices = (await _invoiceRepository.GetInvoicesByStudentIdAsync(studentId)).ToList();
            await SyncInvoiceStatusesAsync(invoices);
            return invoices.Select(MapToDto);
        }

        private async Task SyncInvoiceStatusesAsync(IEnumerable<Invoice> invoices)
        {
            bool hasChanges = false;
            foreach (var i in invoices)
            {
                decimal totalPaid = i.Payments != null
                    ? i.Payments.Where(p => p.Status == "Completed").Sum(p => p.Amount)
                    : 0;

                if (totalPaid >= i.TotalAmount && i.Status != "Paid")
                {
                    i.Status = "Paid";
                    if (!i.PaidAt.HasValue) i.PaidAt = DateTime.UtcNow;
                    await _invoiceRepository.UpdateAsync(i);
                    hasChanges = true;
                }
                else if (totalPaid < i.TotalAmount)
                {
                    if (i.DueDate.Date < DateTime.Today.Date && i.Status != "Overdue")
                    {
                        i.Status = "Overdue";
                        await _invoiceRepository.UpdateAsync(i);
                        hasChanges = true;
                    }

                    // Notification Logic
                    if (i.Student?.User != null || i.Student != null)
                    {
                        int userId = i.Student?.User?.UserId ?? i.Student?.UserId ?? 0;
                        if (userId > 0)
                        {
                            double daysUntilDue = (i.DueDate.Date - DateTime.Today.Date).TotalDays;

                            // 1. Nhắc nợ trước hạn 3 ngày (hoặc còn <= 3 ngày mà chưa gửi)
                            if (daysUntilDue >= 0 && daysUntilDue <= 3 && !i.IsDueReminderSent)
                            {
                                await _notificationService.SendNotificationAsync(
                                    userId,
                                    "Hóa đơn KTX sắp đến hạn",
                                    $"Hóa đơn {i.InvoiceCode} của bạn sẽ hết hạn thanh toán vào ngày {i.DueDate:dd/MM/yyyy}. Vui lòng thanh toán sớm để tránh bị tính phí phạt.");
                                
                                i.IsDueReminderSent = true;
                                await _invoiceRepository.UpdateAsync(i);
                                hasChanges = true;
                            }

                            // 2. Nhắc nợ quá hạn 3 ngày (trễ >= 3 ngày mà chưa gửi)
                            if (daysUntilDue <= -3 && !i.IsOverdueReminderSent)
                            {
                                await _notificationService.SendNotificationAsync(
                                    userId,
                                    "⚠️ Hóa đơn KTX quá hạn thanh toán",
                                    $"Hóa đơn {i.InvoiceCode} của bạn đã quá hạn thanh toán {Math.Abs(daysUntilDue)} ngày! Yêu cầu thanh toán ngay lập tức.");
                                
                                i.IsOverdueReminderSent = true;
                                await _invoiceRepository.UpdateAsync(i);
                                hasChanges = true;
                            }
                        }
                    }
                }
            }

            if (hasChanges)
            {
                await _invoiceRepository.SaveChangesAsync();
            }
        }

        public async Task<InvoiceDetailDto?> GetInvoiceDetailsAsync(int invoiceId)
        {
            var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(invoiceId);
            if (invoice == null) return null;

            decimal totalPaid = invoice.Payments
                .Where(p => p.Status == "Completed")
                .Sum(p => p.Amount);

            return new InvoiceDetailDto
            {
                Id = invoice.InvoiceId,
                InvoiceCode = invoice.InvoiceCode,
                StudentId = invoice.StudentId,
                StudentName = invoice.Student?.FullName ?? "Unknown",
                StudentCode = invoice.Student?.StudentCode ?? "N/A",
                StudentPhone = invoice.Student?.Phone ?? "N/A",
                RoomId = invoice.RoomId,
                RoomNumber = invoice.Room?.RoomNumber ?? "N/A",
                BuildingName = invoice.Room?.Building?.BuildingName ?? "N/A",
                Month = invoice.BillingMonth.Month,
                Year = invoice.BillingMonth.Year,
                BillingMonth = invoice.BillingMonth,
                RoomFee = invoice.RoomFee,
                ElectricityFee = invoice.ServiceFee,
                WaterFee = invoice.OtherFee,
                OtherFee = 0,
                DiscountAmount = invoice.DiscountAmount,
                TotalAmount = invoice.TotalAmount,
                TotalPaid = totalPaid,
                DueDate = invoice.DueDate,
                PaidAt = invoice.PaidAt,
                Status = invoice.Status,
                Note = invoice.Note ?? string.Empty,
                CreatedAt = invoice.CreatedAt,
                Payments = invoice.Payments.Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    PaymentCode = p.PaymentCode,
                    InvoiceId = p.InvoiceId,
                    InvoiceCode = invoice.InvoiceCode,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    TransactionReference = p.TransactionReference,
                    PaymentDate = p.PaymentDate,
                    ReceiverName = p.Receiver?.Username ?? "Hệ thống",
                    Status = p.Status,
                    Note = p.Note
                }).ToList()
            };
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

        public async Task<ServiceResult<bool>> DeleteInvoiceAsync(int invoiceId)
        {
            var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(invoiceId);
            if (invoice == null)
                return ServiceResult<bool>.Failure("Không tìm thấy hóa đơn cần xóa.");

            if (invoice.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<bool>.Failure("Không thể xóa hóa đơn đã thanh toán.");
            }

            if (invoice.Status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<bool>.Failure("Không thể xóa hóa đơn đang trong trạng thái quá hạn.");
            }

            if (invoice.Payments != null && invoice.Payments.Any())
            {
                return ServiceResult<bool>.Failure("Không thể xóa hóa đơn này vì đã có giao dịch thanh toán.");
            }

            try
            {
                await _invoiceRepository.DeleteAsync(invoice);
                await _invoiceRepository.SaveChangesAsync();
                return ServiceResult<bool>.Success(true, "Xóa hóa đơn thành công!");
            }
            catch (Exception)
            {
                return ServiceResult<bool>.Failure("Không thể xóa hóa đơn này vì đã có dữ liệu liên quan.");
            }
        }

        private static InvoiceDto MapToDto(Invoice i)
        {
            decimal totalPaid = i.Payments != null
                ? i.Payments.Where(p => p.Status == "Completed").Sum(p => p.Amount)
                : 0;

            string status = i.Status;
            if (totalPaid >= i.TotalAmount)
            {
                status = "Paid";
            }
            else if (totalPaid > 0)
            {
                status = "PartiallyPaid";
            }
            else if (i.DueDate.Date < DateTime.Today.Date)
            {
                status = "Overdue";
            }

            return new InvoiceDto
            {
                Id = i.InvoiceId,
                InvoiceCode = i.InvoiceCode,
                StudentId = i.StudentId,
                StudentName = i.Student?.FullName ?? "Unknown",
                StudentCode = i.Student?.StudentCode ?? "N/A",
                RoomId = i.RoomId,
                RoomNumber = i.Room?.RoomNumber ?? "N/A",
                BuildingName = i.Room?.Building?.BuildingName ?? "N/A",
                Month = i.BillingMonth.Month,
                Year = i.BillingMonth.Year,
                RoomFee = i.RoomFee,
                ElectricityFee = i.ServiceFee,
                WaterFee = i.OtherFee,
                OtherFee = 0,
                DiscountAmount = i.DiscountAmount,
                TotalAmount = i.TotalAmount,
                TotalPaid = totalPaid,
                DueDate = i.DueDate,
                PaidAt = i.PaidAt,
                Status = status,
                Note = i.Note ?? string.Empty,
                CreatedAt = i.CreatedAt
            };
        }
    }
}

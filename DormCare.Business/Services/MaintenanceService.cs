using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class MaintenanceService
    {
        private readonly MaintenanceRepository _repository;
        private readonly NotificationService _notificationService;

        public event EventHandler? MaintenanceUpdated;

        public MaintenanceService(MaintenanceRepository repository, NotificationService notificationService)
        {
            _repository = repository;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<MaintenanceRequest>> GetAllRequestsAsync()
        {
            return await _repository.GetRequestsWithDetailsAsync();
        }

        public async Task<IEnumerable<MaintenanceRequest>> GetRequestsByStudentIdAsync(int studentId)
        {
            return await _repository.GetRequestsByStudentIdAsync(studentId);
        }

        public async Task<ServiceResult<bool>> CreateRequestAsync(int studentId, int roomId, string title, string description)
        {
            var req = new MaintenanceRequest
            {
                RequestCode = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{studentId}",
                StudentId = studentId,
                RoomId = roomId,
                Category = "Furniture",
                Title = title,
                Description = description,
                Priority = "Medium",
                Status = "Submitted",
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(req);
            await _repository.SaveChangesAsync();

            MaintenanceUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Gửi yêu cầu sửa chữa thành công!");
        }

        public async Task<ServiceResult<bool>> UpdateStatusAsync(int requestId, string newStatus, string resolutionNote)
        {
            var req = await _repository.GetByIdAsync(requestId);
            if (req == null) return ServiceResult<bool>.Failure("Không tìm thấy yêu cầu.");

            req.Status = newStatus;
            req.ResolutionNote = resolutionNote;
            if (newStatus == "Resolved" || newStatus == "Closed")
            {
                req.ResolvedAt = DateTime.UtcNow;
            }

            await _repository.UpdateAsync(req);
            await _repository.SaveChangesAsync();

            // Gửi thông báo cho sinh viên
            await _notificationService.SendNotificationAsync(req.StudentId, "Cập nhật yêu cầu sửa chữa", $"Yêu cầu '{req.Title}' của bạn đã được chuyển sang trạng thái: {newStatus}. Ghi chú: {resolutionNote}");

            MaintenanceUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Cập nhật trạng thái sửa chữa thành công!");
        }

        public async Task<ServiceResult<bool>> UpdatePriorityAsync(int requestId, string newPriority)
        {
            var req = await _repository.GetByIdAsync(requestId);
            if (req == null) return ServiceResult<bool>.Failure("Không tìm thấy yêu cầu.");

            req.Priority = newPriority;
            await _repository.UpdateAsync(req);
            await _repository.SaveChangesAsync();

            MaintenanceUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Cập nhật mức độ ưu tiên thành công!");
        }

        public async Task<ServiceResult<bool>> CloseRequestAsync(int requestId, string resolutionNote)
        {
            var req = await _repository.GetByIdAsync(requestId);
            if (req == null) return ServiceResult<bool>.Failure("Không tìm thấy yêu cầu.");

            req.Status = "Closed";
            req.ResolutionNote = resolutionNote;
            req.ClosedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(req);
            await _repository.SaveChangesAsync();

            // Gửi thông báo cho sinh viên
            await _notificationService.SendNotificationAsync(req.StudentId, "Đóng yêu cầu sửa chữa", $"Yêu cầu '{req.Title}' của bạn đã được đóng. Ghi chú: {resolutionNote}");

            MaintenanceUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Đóng yêu cầu sửa chữa thành công!");
        }
    }
}

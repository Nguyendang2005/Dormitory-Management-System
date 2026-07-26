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

        public event EventHandler? MaintenanceUpdated;

        public MaintenanceService(MaintenanceRepository repository)
        {
            _repository = repository;
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

            MaintenanceUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Cập nhật trạng thái sửa chữa thành công!");
        }
    }
}

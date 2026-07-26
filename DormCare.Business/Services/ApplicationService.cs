using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class ApplicationService
    {
        private readonly DormCareDbContext _context;

        public event EventHandler? ApplicationUpdated;

        public ApplicationService(DormCareDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RoomApplication>> GetAllApplicationsAsync()
        {
            return await _context.RoomApplications
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Room)
                    .ThenInclude(r => r.Building)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();
        }

        public async Task<ServiceResult<bool>> CreateApplicationAsync(int studentId, int roomId, string reason)
        {
            var existingPending = await _context.RoomApplications
                .FirstOrDefaultAsync(a => a.StudentId == studentId && a.Status == "Pending");

            if (existingPending != null)
            {
                return ServiceResult<bool>.Failure("Bạn đã có 1 đơn đăng ký đang chờ duyệt.");
            }

            var app = new RoomApplication
            {
                ApplicationCode = $"APP-{DateTime.UtcNow:yyyyMMdd}-{studentId}",
                StudentId = studentId,
                RoomId = roomId,
                Reason = reason,
                Status = "Pending",
                ApplicationDate = DateTime.UtcNow
            };

            _context.RoomApplications.Add(app);
            await _context.SaveChangesAsync();

            ApplicationUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Gửi đơn đăng ký thuê phòng thành công!");
        }

        public async Task<ServiceResult<bool>> SubmitApplicationAsync(int studentId, int roomId, int? preferredBedId, string reason)
        {
            return await CreateApplicationAsync(studentId, roomId, reason);
        }

        public async Task<ServiceResult<bool>> ApproveApplicationAsync(int applicationId, int reviewerId, string reviewNote)
        {
            var app = await _context.RoomApplications
                .Include(a => a.Room)
                    .ThenInclude(r => r.Beds)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (app == null)
            {
                return ServiceResult<bool>.Failure("Đơn đăng ký không tồn tại.");
            }

            if (app.Status != "Pending")
            {
                return ServiceResult<bool>.Failure("Đơn đăng ký đã xử lý trước đó.");
            }

            app.Status = "Approved";
            app.ReviewedBy = reviewerId;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewNote = reviewNote;

            await _context.SaveChangesAsync();
            ApplicationUpdated?.Invoke(this, EventArgs.Empty);

            return ServiceResult<bool>.Success(true, "Đã phê duyệt đơn đăng ký!");
        }

        public async Task<ServiceResult<bool>> RejectApplicationAsync(int applicationId, int reviewerId, string reviewNote)
        {
            var app = await _context.RoomApplications.FindAsync(applicationId);
            if (app == null)
            {
                return ServiceResult<bool>.Failure("Đơn đăng ký không tồn tại.");
            }

            app.Status = "Rejected";
            app.ReviewedBy = reviewerId;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewNote = reviewNote;

            await _context.SaveChangesAsync();
            ApplicationUpdated?.Invoke(this, EventArgs.Empty);

            return ServiceResult<bool>.Success(true, "Đã từ chối đơn đăng ký.");
        }
    }
}

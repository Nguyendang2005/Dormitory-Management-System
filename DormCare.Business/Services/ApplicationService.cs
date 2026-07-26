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

        public async Task<ServiceResult<bool>> ApproveApplicationAsync(int applicationId, int reviewerUserId, string reviewNote)
        {
            var app = await _context.RoomApplications
                .Include(a => a.Student)
                .Include(a => a.Room)
                    .ThenInclude(r => r.Beds)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (app == null) return ServiceResult<bool>.Failure("Không tìm thấy đơn đăng ký.");

            var availableBed = app.Room.Beds.FirstOrDefault(b => b.Status == "Available");
            if (availableBed == null)
            {
                return ServiceResult<bool>.Failure("Phòng này đã hết giường trống!");
            }

            app.Status = "Approved";
            app.ReviewedBy = reviewerUserId;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewNote = reviewNote;

            availableBed.Status = "Occupied";

            var assignment = new RoomAssignment
            {
                StudentId = app.StudentId,
                RoomId = app.RoomId,
                BedId = availableBed.BedId,
                StartDate = DateTime.UtcNow.Date,
                AssignmentType = "InitialAssignment",
                Status = "Active",
                AssignedBy = reviewerUserId,
                Note = "Duyệt đơn đăng ký tự động"
            };

            _context.RoomAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            ApplicationUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Duyệt đơn đăng ký thành công!");
        }

        public async Task<ServiceResult<bool>> RejectApplicationAsync(int applicationId, int reviewerUserId, string reviewNote)
        {
            var app = await _context.RoomApplications.FindAsync(applicationId);
            if (app == null) return ServiceResult<bool>.Failure("Không tìm thấy đơn đăng ký.");

            app.Status = "Rejected";
            app.ReviewedBy = reviewerUserId;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewNote = reviewNote;

            await _context.SaveChangesAsync();
            ApplicationUpdated?.Invoke(this, EventArgs.Empty);

            return ServiceResult<bool>.Success(true, "Đã từ chối đơn đăng ký.");
        }
    }
}

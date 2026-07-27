using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
                .Include(a => a.PreferredBed)
                .Include(a => a.Reviewer)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RoomApplication>> GetStudentApplicationsAsync(int studentId)
        {
            return await _context.RoomApplications
                .Include(a => a.Room)
                    .ThenInclude(r => r.Building)
                .Include(a => a.PreferredBed)
                .Include(a => a.Reviewer)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BedDto>> GetAvailableBedsByRoomAsync(int roomId)
        {
            return await _context.Beds
                .Include(b => b.Room)
                    .ThenInclude(r => r.Building)
                .Where(b => b.RoomId == roomId && b.Status == "Available")
                .OrderBy(b => b.BedNumber)
                .Select(b => new BedDto
                {
                    BedId = b.BedId,
                    RoomId = b.RoomId,
                    RoomNumber = b.Room.RoomNumber,
                    BuildingName = b.Room.Building.BuildingName,
                    BedNumber = b.BedNumber,
                    BedCode = b.BedCode,
                    Status = b.Status,
                    Description = b.Description ?? string.Empty
                })
                .ToListAsync();
        }

        public Task<ServiceResult<bool>> CreateApplicationAsync(int studentId, int roomId, string reason)
        {
            return SubmitApplicationAsync(studentId, roomId, null, reason);
        }

        public async Task<ServiceResult<bool>> SubmitApplicationAsync(int studentId, int roomId, int? preferredBedId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return ServiceResult<bool>.Failure("Vui long nhap ly do dang ky phong.");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId && s.Status == "Active");
            if (student == null)
            {
                return ServiceResult<bool>.Failure("Khong tim thay sinh vien dang hoat dong.");
            }

            var activeAssignment = await _context.RoomAssignments
                .AnyAsync(a => a.StudentId == studentId && a.Status == "Active");
            if (activeAssignment)
            {
                return ServiceResult<bool>.Failure("Sinh vien da co cho o dang hoat dong.");
            }

            var existingOpenApplication = await _context.RoomApplications
                .AnyAsync(a => a.StudentId == studentId && (a.Status == "Pending" || a.Status == "Approved"));
            if (existingOpenApplication)
            {
                return ServiceResult<bool>.Failure("Sinh vien da co don dang ky dang cho duyet hoac da duoc duyet.");
            }

            var room = await _context.Rooms
                .Include(r => r.Beds)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || room.Status != "Available")
            {
                return ServiceResult<bool>.Failure("Phong khong ton tai hoac khong san sang nhan dang ky.");
            }

            if (room.GenderType != "Mixed" && !room.GenderType.Equals(student.Gender, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<bool>.Failure("Phong khong phu hop gioi tinh cua sinh vien.");
            }

            var availableBeds = room.Beds.Where(b => b.Status == "Available").ToList();
            if (availableBeds.Count == 0)
            {
                return ServiceResult<bool>.Failure("Phong da het giuong trong.");
            }

            if (preferredBedId.HasValue && !availableBeds.Any(b => b.BedId == preferredBedId.Value))
            {
                return ServiceResult<bool>.Failure("Giuong mong muon khong thuoc phong nay hoac khong con trong.");
            }

            var now = DateTime.UtcNow;
            var app = new RoomApplication
            {
                ApplicationCode = BuildApplicationCode(studentId, now),
                StudentId = studentId,
                RoomId = roomId,
                PreferredBedId = preferredBedId,
                Reason = reason.Trim(),
                Status = "Pending",
                ApplicationDate = now,
                CreatedAt = now
            };

            _context.RoomApplications.Add(app);
            await _context.SaveChangesAsync();

            ApplicationUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, "Gui yeu cau dang ky phong thanh cong.");
        }

        public async Task<ServiceResult<bool>> ApproveApplicationAsync(int applicationId, int reviewerId, string reviewNote)
        {
            return await ApproveApplicationAsync(applicationId, reviewerId, reviewNote, null);
        }

        public async Task<ServiceResult<bool>> ApproveApplicationAsync(int applicationId, int reviewerId, string reviewNote, int? selectedBedId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var reviewerIsManager = await _context.Users.AnyAsync(u => u.UserId == reviewerId && u.Role == "Manager" && u.IsActive);
                if (!reviewerIsManager)
                {
                    return ServiceResult<bool>.Failure("Tai khoan khong co quyen duyet don.");
                }

                var app = await _context.RoomApplications
                    .Include(a => a.Student)
                    .Include(a => a.Room)
                        .ThenInclude(r => r.Beds)
                    .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

                if (app == null)
                {
                    return ServiceResult<bool>.Failure("Don dang ky khong ton tai.");
                }

                if (app.Status != "Pending")
                {
                    return ServiceResult<bool>.Failure("Don dang ky da duoc xu ly truoc do.");
                }

                var hasActiveAssignment = await _context.RoomAssignments
                    .AnyAsync(a => a.StudentId == app.StudentId && a.Status == "Active");
                if (hasActiveAssignment)
                {
                    return ServiceResult<bool>.Failure("Sinh vien da co cho o dang hoat dong.");
                }

                var hasApprovedApplication = await _context.RoomApplications
                    .AnyAsync(a => a.ApplicationId != applicationId && a.StudentId == app.StudentId && a.Status == "Approved");
                if (hasApprovedApplication)
                {
                    return ServiceResult<bool>.Failure("Sinh vien da co don khac duoc duyet.");
                }

                if (app.Room.Status != "Available")
                {
                    return ServiceResult<bool>.Failure("Phong khong con san sang de duyet.");
                }

                if (app.Room.GenderType != "Mixed" && !app.Room.GenderType.Equals(app.Student.Gender, StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult<bool>.Failure("Phong khong phu hop gioi tinh cua sinh vien.");
                }

                var bedId = selectedBedId ?? app.PreferredBedId;
                if (!bedId.HasValue)
                {
                    return ServiceResult<bool>.Failure("Vui long chon giuong de duyet don.");
                }

                var bed = await _context.Beds.FirstOrDefaultAsync(b => b.BedId == bedId.Value);
                if (bed == null || bed.RoomId != app.RoomId)
                {
                    return ServiceResult<bool>.Failure("Giuong khong thuoc phong dang ky.");
                }

                if (bed.Status != "Available")
                {
                    return ServiceResult<bool>.Failure("Giuong da duoc su dung, giu cho hoac bao tri.");
                }

                var now = DateTime.UtcNow;
                bed.Status = "Reserved";
                bed.UpdatedAt = now;
                app.PreferredBedId = bed.BedId;
                app.Status = "Approved";
                app.ReviewedBy = reviewerId;
                app.ReviewedAt = now;
                app.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? "Da duyet yeu cau dang ky phong." : reviewNote.Trim();

                if (!app.Room.Beds.Any(b => b.BedId != bed.BedId && b.Status == "Available"))
                {
                    app.Room.Status = "Full";
                    app.Room.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                ApplicationUpdated?.Invoke(this, EventArgs.Empty);

                return ServiceResult<bool>.Success(true, "Da duyet yeu cau va giu cho giuong thanh cong.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ServiceResult<bool>.Failure("Khong the duyet don. Du lieu chua duoc thay doi, vui long thu lai.");
            }
        }

        public async Task<ServiceResult<bool>> RejectApplicationAsync(int applicationId, int reviewerId, string reviewNote)
        {
            if (string.IsNullOrWhiteSpace(reviewNote))
            {
                return ServiceResult<bool>.Failure("Vui long nhap ly do tu choi.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var reviewerIsManager = await _context.Users.AnyAsync(u => u.UserId == reviewerId && u.Role == "Manager" && u.IsActive);
                if (!reviewerIsManager)
                {
                    return ServiceResult<bool>.Failure("Tai khoan khong co quyen tu choi don.");
                }

                var app = await _context.RoomApplications.FindAsync(applicationId);
                if (app == null)
                {
                    return ServiceResult<bool>.Failure("Don dang ky khong ton tai.");
                }

                if (app.Status != "Pending")
                {
                    return ServiceResult<bool>.Failure("Chi co the tu choi don dang cho duyet.");
                }

                app.Status = "Rejected";
                app.ReviewedBy = reviewerId;
                app.ReviewedAt = DateTime.UtcNow;
                app.ReviewNote = reviewNote.Trim();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                ApplicationUpdated?.Invoke(this, EventArgs.Empty);

                return ServiceResult<bool>.Success(true, "Da tu choi yeu cau dang ky.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ServiceResult<bool>.Failure("Khong the tu choi don. Du lieu chua duoc thay doi, vui long thu lai.");
            }
        }

        private static string BuildApplicationCode(int studentId, DateTime now)
        {
            var rawCode = $"APP{now:yyyyMMddHHmmss}{studentId:D4}";
            return rawCode.Length <= 30 ? rawCode : rawCode[^30..];
        }
    }
}

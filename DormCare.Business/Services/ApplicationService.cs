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
                .Where(b =>
                    b.RoomId == roomId &&
                    b.Status == "Available" &&
                    b.Room.Status == "Available" &&
                    b.Room.Building.Status == "Active")
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
                return ServiceResult<bool>.Failure("Vui lòng nhập lý do đăng ký phòng.");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId && s.Status == "Active");
            if (student == null)
            {
                return ServiceResult<bool>.Failure("Không tìm thấy sinh viên đang hoạt động.");
            }

            var activeAssignment = await _context.RoomAssignments
                .Include(a => a.Room)
                .Include(a => a.Bed)
                .FirstOrDefaultAsync(a => a.StudentId == studentId && a.Status == "Active");
            if (activeAssignment != null)
            {
                return ServiceResult<bool>.Failure(BuildActiveAssignmentMessage(activeAssignment));
            }
            var existingOpenApplication = await _context.RoomApplications
                .AnyAsync(a => a.StudentId == studentId && (a.Status == "Pending" || a.Status == "Approved"));
            if (existingOpenApplication)
            {
                return ServiceResult<bool>.Failure("Sinh viên đã có đơn đăng ký đang chờ duyệt hoặc đã được duyệt.");
            }

            var room = await _context.Rooms
                .Include(r => r.Building)
                .Include(r => r.Beds)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || room.Status != "Available")
            {
                return ServiceResult<bool>.Failure("Phòng không tồn tại hoặc không sẵn sàng nhận đăng ký.");
            }

            if (!IsBuildingActive(room.Building))
            {
                return ServiceResult<bool>.Failure("Tòa nhà của phòng đã chọn hiện không hoạt động. Vui lòng chọn phòng khác.");
            }

            if (room.GenderType != "Mixed" && !room.GenderType.Equals(student.Gender, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<bool>.Failure("Phòng không phù hợp với giới tính của sinh viên.");
            }

            var availableBeds = room.Beds.Where(b => b.Status == "Available").ToList();
            if (availableBeds.Count == 0)
            {
                return ServiceResult<bool>.Failure("Phòng đã hết giường trống.");
            }

            if (preferredBedId.HasValue && !availableBeds.Any(b => b.BedId == preferredBedId.Value))
            {
                return ServiceResult<bool>.Failure("Giường mong muốn không thuộc phòng này hoặc không còn trống.");
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
            return ServiceResult<bool>.Success(true, "Gửi yêu cầu đăng ký phòng thành công.");
        }

        public async Task<ServiceResult<bool>> ApproveApplicationAsync(int applicationId, int reviewerId, string reviewNote)
        {
            return await ApproveApplicationAsync(applicationId, reviewerId, reviewNote, null);
        }

        public async Task<ServiceResult<bool>> ApproveApplicationAsync(int applicationId, int reviewerId, string reviewNote, int? selectedBedId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    var reviewerIsManager = await _context.Users.AnyAsync(u => u.UserId == reviewerId && u.Role == "Manager" && u.IsActive);
                    if (!reviewerIsManager)
                    {
                        return ServiceResult<bool>.Failure("Tài khoản không có quyền duyệt đơn.");
                    }

                    var app = await _context.RoomApplications
                        .Include(a => a.Student)
                        .Include(a => a.Room)
                            .ThenInclude(r => r.Building)
                        .Include(a => a.Room)
                            .ThenInclude(r => r.Beds)
                        .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

                    if (app == null)
                    {
                        return ServiceResult<bool>.Failure("Đơn đăng ký không tồn tại.");
                    }

                    if (app.Status != "Pending")
                    {
                        return ServiceResult<bool>.Failure("Đơn đăng ký đã được xử lý trước đó.");
                    }

                    var activeAssignmentForApproval = await _context.RoomAssignments
                        .Include(a => a.Room)
                        .Include(a => a.Bed)
                        .FirstOrDefaultAsync(a => a.StudentId == app.StudentId && a.Status == "Active");
                    if (activeAssignmentForApproval != null)
                    {
                        return ServiceResult<bool>.Failure(BuildActiveAssignmentMessage(activeAssignmentForApproval));
                    }

                    var hasApprovedApplication = await _context.RoomApplications
                        .AnyAsync(a => a.ApplicationId != applicationId && a.StudentId == app.StudentId && a.Status == "Approved");
                    if (hasApprovedApplication)
                    {
                        return ServiceResult<bool>.Failure("Sinh viên đã có đơn khác được duyệt.");
                    }

                    if (app.Room.Status != "Available")
                    {
                        return ServiceResult<bool>.Failure("Phòng không còn sẵn sàng để duyệt.");
                    }

                    if (!IsBuildingActive(app.Room.Building))
                    {
                        return ServiceResult<bool>.Failure("Không thể duyệt đơn vì tòa nhà hiện không hoạt động.");
                    }

                    if (app.Room.GenderType != "Mixed" && !app.Room.GenderType.Equals(app.Student.Gender, StringComparison.OrdinalIgnoreCase))
                    {
                        return ServiceResult<bool>.Failure("Phòng không phù hợp với giới tính của sinh viên.");
                    }

                    var bedId = selectedBedId ?? app.PreferredBedId;
                    if (!bedId.HasValue)
                    {
                        return ServiceResult<bool>.Failure("Vui lòng chọn giường để duyệt đơn.");
                    }

                    var bed = await _context.Beds.FirstOrDefaultAsync(b => b.BedId == bedId.Value);
                    if (bed == null || bed.RoomId != app.RoomId)
                    {
                        return ServiceResult<bool>.Failure("Giường không thuộc phòng đăng ký.");
                    }

                    if (bed.Status != "Available")
                    {
                        return ServiceResult<bool>.Failure("Giường đã được sử dụng, giữ chỗ hoặc bảo trì.");
                    }

                    var now = DateTime.UtcNow;
                    bed.Status = "Occupied";
                    bed.UpdatedAt = now;
                    app.PreferredBedId = bed.BedId;
                    app.Status = "Approved";
                    app.ReviewedBy = reviewerId;
                    app.ReviewedAt = now;
                    app.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? "Đã duyệt yêu cầu đăng ký phòng." : reviewNote.Trim();

                    // 1. Tạo RoomAssignment hoạt động (Active) cho sinh viên
                    var assignment = new RoomAssignment
                    {
                        StudentId = app.StudentId,
                        RoomId = app.RoomId,
                        BedId = bed.BedId,
                        StartDate = now.Date,
                        AssignmentType = "InitialAssignment",
                        Status = "Active",
                        AssignedBy = reviewerId,
                        Note = $"Check-in xếp phòng theo đơn đăng ký #{app.ApplicationCode}",
                        CreatedAt = now
                    };
                    _context.RoomAssignments.Add(assignment);

                    // 2. Cập nhật trạng thái phòng nếu không còn giường trống
                    if (!app.Room.Beds.Any(b => b.BedId != bed.BedId && b.Status == "Available"))
                    {
                        app.Room.Status = "Full";
                        app.Room.UpdatedAt = now;
                    }

                    // 3. Gửi thông báo cho sinh viên
                    if (app.Student != null)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = app.Student.UserId,
                            Title = "Đơn đăng ký phòng đã được duyệt",
                            Message = $"Chúc mừng! Yêu cầu đăng ký phòng của bạn đã được duyệt. Bạn đã được xếp vào giường {bed.BedCode} (Phòng {app.Room.RoomNumber}).",
                            NotificationType = "ApplicationApproved",
                            CreatedAt = now
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    ApplicationUpdated?.Invoke(this, EventArgs.Empty);

                    return ServiceResult<bool>.Success(true, $"Đã duyệt đơn đăng ký, gán giường {bed.BedCode} và xếp chỗ ở cho sinh viên thành công.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    string detail = ex.InnerException?.Message ?? ex.Message;
                    return ServiceResult<bool>.Failure($"Không thể duyệt đơn: {detail}");
                }
            });
        }

        public async Task<ServiceResult<bool>> RejectApplicationAsync(int applicationId, int reviewerId, string reviewNote)
        {
            if (string.IsNullOrWhiteSpace(reviewNote))
            {
                return ServiceResult<bool>.Failure("Vui lòng nhập lý do từ chối.");
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    var reviewerIsManager = await _context.Users.AnyAsync(u => u.UserId == reviewerId && u.Role == "Manager" && u.IsActive);
                    if (!reviewerIsManager)
                    {
                        return ServiceResult<bool>.Failure("Tài khoản không có quyền từ chối đơn.");
                    }

                    var app = await _context.RoomApplications.FindAsync(applicationId);
                    if (app == null)
                    {
                        return ServiceResult<bool>.Failure("Đơn đăng ký không tồn tại.");
                    }

                    if (app.Status != "Pending")
                    {
                        return ServiceResult<bool>.Failure("Chỉ có thể từ chối đơn đang chờ duyệt.");
                    }

                    app.Status = "Rejected";
                    app.ReviewedBy = reviewerId;
                    app.ReviewedAt = DateTime.UtcNow;
                    app.ReviewNote = reviewNote.Trim();

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    ApplicationUpdated?.Invoke(this, EventArgs.Empty);

                    return ServiceResult<bool>.Success(true, "Đã từ chối đơn đăng ký thành công.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    string detail = ex.InnerException?.Message ?? ex.Message;
                    return ServiceResult<bool>.Failure($"Không thể từ chối đơn: {detail}");
                }
            });
        }

        private static string BuildApplicationCode(int studentId, DateTime dt)
        {
            return $"APP-{studentId}-{dt:yyyyMMdd-HHmmss}";
        }

        private static string BuildActiveAssignmentMessage(RoomAssignment assignment)
        {
            if (assignment.Room != null && assignment.Bed != null)
            {
                return $"Bạn hiện đang ở phòng {assignment.Room.RoomNumber}, giường {assignment.Bed.BedCode}. Vui lòng check-out phòng hiện tại trước khi đăng ký phòng mới.";
            }

            return "Bạn đang có phòng trong ký túc xá. Vui lòng hoàn thành thủ tục trả phòng trước khi đăng ký phòng mới.";
        }

        private static bool IsBuildingActive(Building? building)
        {
            return building != null && building.Status == "Active";
        }
    }
}

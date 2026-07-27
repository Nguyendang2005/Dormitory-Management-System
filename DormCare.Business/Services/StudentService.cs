using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Data;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class StudentService
    {
        private readonly StudentRepository _studentRepository;
        private readonly DormCareDbContext _context;

        public event EventHandler? StudentUpdated;

        public StudentService(StudentRepository studentRepository, DormCareDbContext context)
        {
            _studentRepository = studentRepository;
            _context = context;
        }

        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _studentRepository.GetStudentsWithDetailsAsync();
            return students.Select(MapToDto).ToList();
        }

        private static StudentDto MapToDto(Student s)
        {
            var activeAssignment = s.RoomAssignments.FirstOrDefault(ra => ra.Status == "Active");
            return new StudentDto
            {
                Id = s.StudentId,
                UserId = s.UserId,
                StudentCode = s.StudentCode,
                FullName = s.FullName,
                DateOfBirth = s.DateOfBirth,
                Major = s.Major,
                ClassName = s.ClassName,
                Gender = s.Gender,
                Email = s.Email,
                PhoneNumber = s.Phone,
                Campus = s.Campus,
                Address = s.Address,
                EmergencyContactName = s.EmergencyContactName,
                EmergencyContactPhone = s.EmergencyContactPhone,
                Status = s.Status,
                RoomId = activeAssignment?.RoomId,
                RoomNumber = activeAssignment?.Room?.RoomNumber ?? "Chưa nhận phòng",
                BuildingName = activeAssignment?.Room?.Building?.BuildingName ?? "N/A",
                BedNumber = activeAssignment?.Bed?.BedNumber ?? "N/A",
                CheckInDate = activeAssignment?.StartDate
            };
        }

        public async Task<Student?> GetStudentByUserIdAsync(int userId)
        {
            return await _studentRepository.GetStudentByUserIdAsync(userId);
        }

        /* =====================================================
           CREATE
           ===================================================== */

        public async Task<ServiceResult<StudentDto>> CreateStudentAsync(StudentDto dto)
        {
            var validation = Validate(dto);
            if (validation != null) return ServiceResult<StudentDto>.Failure(validation);

            if (await _context.Students.AnyAsync(s => s.StudentCode == dto.StudentCode))
                return ServiceResult<StudentDto>.Failure($"Mã sinh viên '{dto.StudentCode}' đã tồn tại.");

            if (await _context.Students.AnyAsync(s => s.Email == dto.Email) ||
                await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return ServiceResult<StudentDto>.Failure($"Email '{dto.Email}' đã được sử dụng.");

            var username = dto.StudentCode.ToLowerInvariant();
            if (await _context.Users.AnyAsync(u => u.Username == username))
                return ServiceResult<StudentDto>.Failure($"Tên đăng nhập '{username}' đã tồn tại.");

            var user = new User
            {
                Username = username,
                PasswordHash = "123456",
                Email = dto.Email,
                Phone = dto.PhoneNumber,
                Role = "Student",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var student = new Student
            {
                User = user,
                StudentCode = dto.StudentCode.Trim(),
                FullName = dto.FullName.Trim(),
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Email = dto.Email.Trim(),
                Phone = dto.PhoneNumber.Trim(),
                Major = dto.Major.Trim(),
                ClassName = dto.ClassName.Trim(),
                Campus = dto.Campus.Trim(),
                Address = dto.Address,
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            StudentUpdated?.Invoke(this, EventArgs.Empty);
            dto.Id = student.StudentId;
            return ServiceResult<StudentDto>.Success(dto,
                $"Đã thêm sinh viên '{student.FullName}'. Tài khoản đăng nhập: {username} / 123456");
        }

        /* =====================================================
           UPDATE
           ===================================================== */

        public async Task<ServiceResult<StudentDto>> UpdateStudentAsync(StudentDto dto)
        {
            var validation = Validate(dto);
            if (validation != null) return ServiceResult<StudentDto>.Failure(validation);

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == dto.Id);

            if (student == null)
                return ServiceResult<StudentDto>.Failure("Không tìm thấy sinh viên.");

            if (await _context.Students.AnyAsync(s => s.StudentCode == dto.StudentCode && s.StudentId != dto.Id))
                return ServiceResult<StudentDto>.Failure($"Mã sinh viên '{dto.StudentCode}' đã tồn tại.");

            if (await _context.Students.AnyAsync(s => s.Email == dto.Email && s.StudentId != dto.Id) ||
                await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != student.UserId))
                return ServiceResult<StudentDto>.Failure($"Email '{dto.Email}' đã được sử dụng.");

            student.StudentCode = dto.StudentCode.Trim();
            student.FullName = dto.FullName.Trim();
            student.DateOfBirth = dto.DateOfBirth;
            student.Gender = dto.Gender;
            student.Email = dto.Email.Trim();
            student.Phone = dto.PhoneNumber.Trim();
            student.Major = dto.Major.Trim();
            student.ClassName = dto.ClassName.Trim();
            student.Campus = dto.Campus.Trim();
            student.Address = dto.Address;
            student.EmergencyContactName = dto.EmergencyContactName;
            student.EmergencyContactPhone = dto.EmergencyContactPhone;
            student.Status = dto.Status;
            student.UpdatedAt = DateTime.UtcNow;

            student.User.Email = dto.Email.Trim();
            student.User.Phone = dto.PhoneNumber.Trim();
            student.User.IsActive = dto.Status == "Active";
            student.User.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            StudentUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<StudentDto>.Success(dto, $"Đã cập nhật thông tin sinh viên '{student.FullName}'.");
        }

        /* =====================================================
           DELETE
           ===================================================== */

        public async Task<ServiceResult<bool>> DeleteStudentAsync(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return ServiceResult<bool>.Failure("Không tìm thấy sinh viên.");

            var hasActiveAssignment = await _context.RoomAssignments
                .AnyAsync(ra => ra.StudentId == studentId && ra.Status == "Active");
            if (hasActiveAssignment)
                return ServiceResult<bool>.Failure(
                    "Sinh viên đang ở trong ký túc xá. Vui lòng Check-out trước khi xóa.");

            var hasHistory =
                await _context.RoomAssignments.AnyAsync(ra => ra.StudentId == studentId) ||
                await _context.Invoices.AnyAsync(i => i.StudentId == studentId) ||
                await _context.RoomApplications.AnyAsync(a => a.StudentId == studentId) ||
                await _context.MaintenanceRequests.AnyAsync(m => m.StudentId == studentId);

            if (hasHistory)
                return ServiceResult<bool>.Failure(
                    "Sinh viên đã có dữ liệu liên quan (lịch sử ở, hóa đơn, đơn đăng ký hoặc yêu cầu sửa chữa) nên không thể xóa. " +
                    "Hãy dùng chức năng Sửa và chuyển Trạng thái sang 'Inactive' để ngừng hoạt động tài khoản.");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == student.UserId)
                .ToListAsync();
            _context.Notifications.RemoveRange(notifications);

            _context.Students.Remove(student);
            _context.Users.Remove(student.User);
            await _context.SaveChangesAsync();

            StudentUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true, $"Đã xóa sinh viên '{student.FullName}'.");
        }

        /* =====================================================
           CHECK-IN / CHECK-OUT
           ===================================================== */

        public async Task<IEnumerable<BedDto>> GetAvailableBedsAsync()
        {
            var beds = await _context.Beds
                .Include(b => b.Room)
                    .ThenInclude(r => r.Building)
                .Where(b => b.Status == "Available"
                            && b.Room.Status != "Maintenance"
                            && b.Room.Status != "Inactive")
                .OrderBy(b => b.Room.Building.BuildingName)
                    .ThenBy(b => b.Room.RoomNumber)
                    .ThenBy(b => b.BedNumber)
                .ToListAsync();

            return beds.Select(b => new BedDto
            {
                BedId = b.BedId,
                RoomId = b.RoomId,
                RoomNumber = b.Room.RoomNumber,
                BuildingName = b.Room.Building.BuildingName,
                BedNumber = b.BedNumber,
                BedCode = b.BedCode,
                Status = b.Status,
                Description = b.Description ?? string.Empty
            }).ToList();
        }

        public async Task<ServiceResult<bool>> CheckInAsync(int studentId, int bedId, int managerId, string? note = null)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return ServiceResult<bool>.Failure("Không tìm thấy sinh viên.");

            if (student.Status != "Active")
                return ServiceResult<bool>.Failure("Chỉ sinh viên có trạng thái 'Active' mới được check-in.");

            var hasActive = await _context.RoomAssignments
                .AnyAsync(ra => ra.StudentId == studentId && ra.Status == "Active");
            if (hasActive)
                return ServiceResult<bool>.Failure("Sinh viên đã có phòng ở. Vui lòng check-out trước khi chuyển phòng.");

            var bed = await _context.Beds
                .Include(b => b.Room)
                    .ThenInclude(r => r.Beds)
                .FirstOrDefaultAsync(b => b.BedId == bedId);

            if (bed == null)
                return ServiceResult<bool>.Failure("Không tìm thấy giường.");

            if (bed.Status != "Available")
                return ServiceResult<bool>.Failure($"Giường {bed.BedCode} không còn trống.");

            if (bed.Room.Status == "Maintenance" || bed.Room.Status == "Inactive")
                return ServiceResult<bool>.Failure($"Phòng {bed.Room.RoomNumber} đang bảo trì / ngừng hoạt động.");

            if (bed.Room.GenderType != "Mixed" && bed.Room.GenderType != student.Gender)
                return ServiceResult<bool>.Failure(
                    $"Phòng {bed.Room.RoomNumber} chỉ dành cho sinh viên {(bed.Room.GenderType == "Male" ? "nam" : "nữ")}.");

            var assignment = new RoomAssignment
            {
                StudentId = studentId,
                RoomId = bed.RoomId,
                BedId = bed.BedId,
                StartDate = DateTime.UtcNow.Date,
                AssignmentType = "InitialAssignment",
                Status = "Active",
                AssignedBy = managerId,
                Note = string.IsNullOrWhiteSpace(note) ? "Check-in bởi quản lý" : note,
                CreatedAt = DateTime.UtcNow
            };
            _context.RoomAssignments.Add(assignment);

            bed.Status = "Occupied";
            bed.UpdatedAt = DateTime.UtcNow;

            var stillAvailable = bed.Room.Beds.Any(b => b.BedId != bed.BedId && b.Status == "Available");
            if (!stillAvailable && bed.Room.Status == "Available")
            {
                bed.Room.Status = "Full";
                bed.Room.UpdatedAt = DateTime.UtcNow;
            }

            _context.Notifications.Add(new Notification
            {
                UserId = student.UserId,
                Title = "Check-in thành công",
                Message = $"Bạn đã được xếp vào giường {bed.BedCode} từ ngày {DateTime.Now:dd/MM/yyyy}.",
                NotificationType = "RoomAssignment",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            StudentUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true,
                $"Check-in thành công: '{student.FullName}' → giường {bed.BedCode}.");
        }

        public async Task<ServiceResult<bool>> CheckOutAsync(int studentId, int managerId, string? note = null)
        {
            var assignment = await _context.RoomAssignments
                .Include(ra => ra.Student)
                .Include(ra => ra.Bed)
                .Include(ra => ra.Room)
                .FirstOrDefaultAsync(ra => ra.StudentId == studentId && ra.Status == "Active");

            if (assignment == null)
                return ServiceResult<bool>.Failure("Sinh viên chưa nhận phòng nên không thể check-out.");

            assignment.Status = "Ended";
            assignment.EndDate = DateTime.UtcNow.Date;
            if (!string.IsNullOrWhiteSpace(note))
                assignment.Note = note;

            assignment.Bed.Status = "Available";
            assignment.Bed.UpdatedAt = DateTime.UtcNow;

            if (assignment.Room.Status == "Full")
            {
                assignment.Room.Status = "Available";
                assignment.Room.UpdatedAt = DateTime.UtcNow;
            }

            _context.Notifications.Add(new Notification
            {
                UserId = assignment.Student.UserId,
                Title = "Check-out thành công",
                Message = $"Bạn đã trả giường {assignment.Bed.BedCode} ngày {DateTime.Now:dd/MM/yyyy}.",
                NotificationType = "RoomAssignment",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            StudentUpdated?.Invoke(this, EventArgs.Empty);
            return ServiceResult<bool>.Success(true,
                $"Check-out thành công: '{assignment.Student.FullName}' đã trả giường {assignment.Bed.BedCode}.");
        }

        /* =====================================================
           VALIDATION
           ===================================================== */

        private static string? Validate(StudentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.StudentCode)) return "Vui lòng nhập Mã sinh viên.";
            if (string.IsNullOrWhiteSpace(dto.FullName)) return "Vui lòng nhập Họ và tên.";
            if (string.IsNullOrWhiteSpace(dto.Email)) return "Vui lòng nhập Email.";
            if (!dto.Email.Contains('@')) return "Email không hợp lệ.";
            if (string.IsNullOrWhiteSpace(dto.PhoneNumber)) return "Vui lòng nhập Số điện thoại.";
            if (string.IsNullOrWhiteSpace(dto.Major)) return "Vui lòng nhập Ngành học.";
            if (string.IsNullOrWhiteSpace(dto.ClassName)) return "Vui lòng nhập Lớp.";
            if (string.IsNullOrWhiteSpace(dto.Campus)) return "Vui lòng nhập Cơ sở (Campus).";
            if (dto.DateOfBirth > DateTime.Today.AddYears(-15)) return "Ngày sinh không hợp lệ (sinh viên phải từ 15 tuổi).";
            return null;
        }
    }
}

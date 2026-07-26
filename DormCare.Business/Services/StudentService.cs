using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.DataAccess.Repositories;
using DormCare.Domain.Entities;

namespace DormCare.Business.Services
{
    public class StudentService
    {
        private readonly StudentRepository _studentRepository;

        public StudentService(StudentRepository studentRepository)
        {
            _studentRepository = studentRepository;
        }

        public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _studentRepository.GetStudentsWithDetailsAsync();
            return students.Select(s =>
            {
                var activeAssignment = s.RoomAssignments.FirstOrDefault(ra => ra.Status == "Active");
                return new StudentDto
                {
                    Id = s.StudentId,
                    UserId = s.UserId,
                    StudentCode = s.StudentCode,
                    FullName = s.FullName,
                    Major = s.Major,
                    ClassName = s.ClassName,
                    Gender = s.Gender,
                    Email = s.Email,
                    PhoneNumber = s.Phone,
                    RoomNumber = activeAssignment?.Room?.RoomNumber ?? "Chưa nhận phòng",
                    BuildingName = activeAssignment?.Room?.Building?.BuildingName ?? "N/A",
                    BedNumber = activeAssignment?.Bed?.BedNumber ?? "N/A"
                };
            });
        }

        public async Task<Student?> GetStudentByUserIdAsync(int userId)
        {
            return await _studentRepository.GetStudentByUserIdAsync(userId);
        }
    }
}

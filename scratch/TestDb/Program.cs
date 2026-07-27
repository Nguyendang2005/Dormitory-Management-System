using System;
using System.Linq;
using System.Threading.Tasks;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.DataAccess.Data;
using DormCare.DataAccess.Repositories;

class Program
{
    static async Task Main()
    {
        try
        {
            using var context = new DormCareDbContext();
            var repo = new StudentRepository(context);
            var service = new StudentService(repo, context);

            // 1. CREATE
            var dto = new StudentDto
            {
                StudentCode = "TEST9999",
                FullName = "Sinh Viên Thử Nghiệm",
                DateOfBirth = new DateTime(2005, 5, 5),
                Gender = "Male",
                Email = "test9999@fpt.edu.vn",
                PhoneNumber = "0900999999",
                Major = "Software Engineering",
                ClassName = "SE9999",
                Campus = "FPT University Da Nang",
                Status = "Active"
            };
            var created = await service.CreateStudentAsync(dto);
            Console.WriteLine($"CREATE: {created.IsSuccess} - {created.Message}");
            if (!created.IsSuccess) return;
            var id = created.Data!.Id;

            // 2. UPDATE
            dto.FullName = "Sinh Viên Đã Sửa";
            var updated = await service.UpdateStudentAsync(dto);
            Console.WriteLine($"UPDATE: {updated.IsSuccess} - {updated.Message}");

            // 3. CHECK-IN
            var beds = (await service.GetAvailableBedsAsync()).ToList();
            Console.WriteLine($"Available beds: {beds.Count}");
            var maleBed = beds.FirstOrDefault(b => b.BuildingName.Contains("A") || b.BuildingName.Contains("C"));
            if (maleBed != null)
            {
                var checkin = await service.CheckInAsync(id, maleBed.BedId, 1, "test check-in");
                Console.WriteLine($"CHECK-IN ({maleBed.BedCode}): {checkin.IsSuccess} - {checkin.Message}");

                // 4. CHECK-OUT
                var checkout = await service.CheckOutAsync(id, 1, "test check-out");
                Console.WriteLine($"CHECK-OUT: {checkout.IsSuccess} - {checkout.Message}");
            }

            // 5. DELETE (has assignment history now -> should be blocked)
            var deleted = await service.DeleteStudentAsync(id);
            Console.WriteLine($"DELETE (expect blocked): {deleted.IsSuccess} - {deleted.Message}");

            // Cleanup test data manually
            var student = await context.Students.FindAsync(id);
            if (student != null)
            {
                var user = await context.Users.FindAsync(student.UserId);
                context.RoomAssignments.RemoveRange(context.RoomAssignments.Where(ra => ra.StudentId == id));
                context.Notifications.RemoveRange(context.Notifications.Where(n => n.UserId == student.UserId));
                context.Students.Remove(student);
                if (user != null) context.Users.Remove(user);
                await context.SaveChangesAsync();
                Console.WriteLine("Cleanup: removed test student.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("EXCEPTION: " + ex);
        }
    }
}

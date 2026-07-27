using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.DataAccess.Data
{
    public static class DbInitializer
    {
        public static void Initialize(DormCareDbContext context)
        {
            if (context.Database.CanConnect())
            {
                if (!context.Users.Any(u => u.Username == "admin"))
                {
                    context.Users.Add(new User
                    {
                        Username = "admin",
                        PasswordHash = "admin123",
                        Email = "admin@dormcare.com",
                        Role = "Manager",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                var studentUser = context.Users.FirstOrDefault(u => u.Username == "student1");
                if (studentUser == null)
                {
                    studentUser = new User
                    {
                        Username = "student1",
                        PasswordHash = "student123",
                        Email = "student1@dormcare.com",
                        Role = "Student",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Users.Add(studentUser);
                    context.SaveChanges();
                }

                if (!context.Students.Any(s => s.UserId == studentUser.UserId))
                {
                    context.Students.Add(new Student
                    {
                        UserId = studentUser.UserId,
                        StudentCode = "SV001",
                        FullName = "Nguyễn Văn A",
                        DateOfBirth = new DateTime(2004, 1, 1),
                        Gender = "Male",
                        Email = "student1@dormcare.com",
                        Phone = "0987654321",
                        Major = "Công nghệ thông tin",
                        ClassName = "IT1",
                        Campus = "Hòa Lạc",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    });
                    context.SaveChanges();
                }
            }
        }

        public static async Task InitializeAsync(DormCareDbContext context)
        {
            if (await context.Database.CanConnectAsync())
            {
                try
                {
                    // Thoroughly strip all repeated (Đã cập nhật) suffixes from BuildingName in SQL Server
                    await context.Database.ExecuteSqlRawAsync(@"
                        WHILE EXISTS (SELECT 1 FROM Buildings WHERE BuildingName LIKE '%(Đã cập nhật)%')
                        BEGIN
                            UPDATE Buildings 
                            SET BuildingName = RTRIM(LTRIM(REPLACE(REPLACE(BuildingName, '(Đã cập nhật)', ''), '  ', ' ')))
                            WHERE BuildingName LIKE '%(Đã cập nhật)%';
                        END
                    ");

                    // Fix invalid RoomType values violating CK_Rooms_Type
                    await context.Database.ExecuteSqlRawAsync(
                        "UPDATE Rooms SET RoomType = 'Premium' WHERE RoomType NOT IN ('Standard', 'Premium', 'Accessible');");
                }
                catch
                {
                    // Ignore cleanup errors if DB table is initializing
                }
            }
        }
    }
}

using System;
using System.Linq;
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
    }
}

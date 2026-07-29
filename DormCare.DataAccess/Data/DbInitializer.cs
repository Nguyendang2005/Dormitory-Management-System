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

                    // Ensure missing columns on Invoices table are automatically added if missing
                    await context.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Invoices') AND name = N'IsDueReminderSent')
                            ALTER TABLE Invoices ADD IsDueReminderSent BIT NOT NULL DEFAULT 0;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Invoices') AND name = N'IsOverdueReminderSent')
                            ALTER TABLE Invoices ADD IsOverdueReminderSent BIT NOT NULL DEFAULT 0;
                    ");

                    // Convert UQ_Invoices_Student_Month to a filtered unique index ignoring Cancelled status
                    await context.Database.ExecuteSqlRawAsync(@"
                        IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'UQ_Invoices_Student_Month')
                            ALTER TABLE Invoices DROP CONSTRAINT UQ_Invoices_Student_Month;

                        IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Invoices_Student_Month' AND object_id = OBJECT_ID('Invoices') AND filter_definition IS NULL)
                            DROP INDEX UQ_Invoices_Student_Month ON Invoices;

                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Invoices_Student_Month' AND object_id = OBJECT_ID('Invoices'))
                            CREATE UNIQUE NONCLUSTERED INDEX UQ_Invoices_Student_Month ON Invoices(StudentId, BillingMonth) WHERE Status <> 'Cancelled';
                    ");

                    // Fix invalid RoomType values violating CK_Rooms_Type
                    await context.Database.ExecuteSqlRawAsync(
                        "UPDATE Rooms SET RoomType = 'Premium' WHERE RoomType NOT IN ('Standard', 'Premium', 'Accessible');");

                    // Auto-sync approved applications that are missing active RoomAssignments
                    await context.Database.ExecuteSqlRawAsync(@"
                        INSERT INTO RoomAssignments (StudentId, RoomId, BedId, StartDate, AssignmentType, Status, AssignedBy, Note, CreatedAt)
                        SELECT a.StudentId, a.RoomId, a.PreferredBedId, COALESCE(a.ReviewedAt, GETUTCDATE()), 'Application', 'Active', COALESCE(a.ReviewedBy, 1), N'Xếp phòng từ đơn đăng ký được duyệt', GETUTCDATE()
                        FROM RoomApplications a
                        WHERE a.Status = 'Approved' AND a.PreferredBedId IS NOT NULL
                          AND NOT EXISTS (SELECT 1 FROM RoomAssignments ra WHERE ra.StudentId = a.StudentId AND ra.Status = 'Active');

                        UPDATE Beds
                        SET Status = 'Occupied', UpdatedAt = GETUTCDATE()
                        WHERE BedId IN (SELECT BedId FROM RoomAssignments WHERE Status = 'Active') AND Status <> 'Occupied';
                    ");
                }
                catch
                {
                    // Ignore cleanup errors if DB table is initializing
                }
            }
        }
    }
}

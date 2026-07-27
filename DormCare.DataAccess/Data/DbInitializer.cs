using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DormCare.DataAccess.Data
{
    public static class DbInitializer
    {
        public static void Initialize(DormCareDbContext context)
        {
            context.Database.CanConnect();
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

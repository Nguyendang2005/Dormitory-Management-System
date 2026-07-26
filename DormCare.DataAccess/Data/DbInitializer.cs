using DormCare.DataAccess.Data;

namespace DormCare.DataAccess.Data
{
    public static class DbInitializer
    {
        public static void Initialize(DormCareDbContext context)
        {
            // Ensure database is available and connected to SQL Server (DormCareDB)
            context.Database.CanConnect();
        }
    }
}

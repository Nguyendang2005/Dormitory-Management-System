using System.Threading.Tasks;

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
            await context.Database.CanConnectAsync();
        }
    }
}

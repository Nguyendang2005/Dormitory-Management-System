using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;

namespace DormCare.DataAccess.Repositories
{
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository(DormCareDbContext context) : base(context) { }

        public async Task<User?> GetByUsernameAsync(string username)
        {

            return await _dbSet
                .Include(u => u.StudentProfile!)
                .FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}

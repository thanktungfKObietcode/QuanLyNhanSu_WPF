using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Repositories
{
    public class UserRepository
    {
        private readonly ApplicationDbContext _db;

        public UserRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _db.Users.Include(u => u.Employee).FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> GetByIdAsync(int id)
        {
            return await _db.Users.FindAsync(id);
        }

        public async Task AddAsync(User user)
        {
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        public async Task<User> GetByEmployeeIdAsync(int employeeId)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.EmployeeID == employeeId);
        }
    }
}

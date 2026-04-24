using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Repositories
{
    public class EmployeeRepository
    {
        private readonly ApplicationDbContext _db;

        public EmployeeRepository(ApplicationDbContext db) => _db = db;

        public async Task<Employee> GetByIdAsync(int id)
            => await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

        public async Task<IEnumerable<Employee>> GetAllAsync()
            => await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .OrderBy(e => e.Name)
                .ToListAsync();

        public async Task<IEnumerable<Employee>> GetActiveAsync()
            => await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.Status == EmployeeStatus.Active)
                .OrderBy(e => e.Name)
                .ToListAsync();

        public async Task<IEnumerable<Employee>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllAsync();

            keyword = keyword.Trim().ToLower();
            return await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.Name.ToLower().Contains(keyword)
                         || e.Code.ToLower().Contains(keyword)
                         || (e.Email != null && e.Email.ToLower().Contains(keyword)))
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Employee>> GetBirthdaysThisMonthAsync()
        {
            var month = DateTime.Today.Month;
            return await _db.Employees
                .Where(e => e.Status == EmployeeStatus.Active && e.DateOfBirth.Month == month)
                .OrderBy(e => e.DateOfBirth.Day)
                .ToListAsync();
        }

        public async Task<int> GetActiveCountAsync()
            => await _db.Employees.CountAsync(e => e.Status == EmployeeStatus.Active);

        public async Task AddAsync(Employee employee)
        {
            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Employee employee)
        {
            _db.Employees.Update(employee);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Employee employee)
        {
            _db.Employees.Remove(employee);
            await _db.SaveChangesAsync();
        }

        public async Task DeactivateAsync(int employeeId)
        {
            var emp = await _db.Employees.FindAsync(employeeId);
            if (emp != null)
            {
                emp.Status = EmployeeStatus.Inactive;
                await _db.SaveChangesAsync();
            }
        }
    }
}

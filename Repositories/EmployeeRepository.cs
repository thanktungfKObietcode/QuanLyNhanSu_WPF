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
        {
            var list = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .OrderBy(e => e.Name)
                .ToListAsync();
            await LoadAccountStatus(list);
            return list;
        }

        public async Task<IEnumerable<Employee>> GetActiveAsync()
        {
            var list = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.Status == EmployeeStatus.Active)
                .OrderBy(e => e.Name)
                .ToListAsync();
            await LoadAccountStatus(list);
            return list;
        }

        public async Task<IEnumerable<Employee>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllAsync();

            keyword = keyword.Trim().ToLower();
            var list = await _db.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Where(e => e.Name.ToLower().Contains(keyword)
                         || e.Code.ToLower().Contains(keyword)
                         || (e.Email != null && e.Email.ToLower().Contains(keyword)))
                .OrderBy(e => e.Name)
                .ToListAsync();
            await LoadAccountStatus(list);
            return list;
        }

        private async Task LoadAccountStatus(IEnumerable<Employee> employees)
        {
            var userEmployeeIds = await _db.Users.Where(u => u.EmployeeID.HasValue).Select(u => u.EmployeeID.Value).ToListAsync();
            foreach (var emp in employees)
            {
                emp.HasAccount = userEmployeeIds.Contains(emp.EmployeeID);
            }
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

        public async Task<bool> ExistsByCodeAsync(string code, int? excludeEmployeeId = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var normalizedCode = code.Trim().ToUpper();
            return await _db.Employees.AnyAsync(e =>
                e.Code.ToUpper() == normalizedCode &&
                (!excludeEmployeeId.HasValue || e.EmployeeID != excludeEmployeeId.Value));
        }

        public Task<bool> DepartmentExistsAsync(int departmentId)
            => _db.Departments.AnyAsync(d => d.DepartmentID == departmentId);

        public Task<bool> PositionExistsAsync(int positionId)
            => _db.Positions.AnyAsync(p => p.PositionID == positionId);

        public async Task AddAsync(Employee employee)
        {
            employee.Department = null;
            employee.Position = null;
            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Employee employee)
        {
            var existing = await _db.Employees.FindAsync(employee.EmployeeID);
            if (existing == null)
            {
                throw new InvalidOperationException($"Khong tim thay nhan vien co ID {employee.EmployeeID}.");
            }

            _db.Entry(existing).CurrentValues.SetValues(employee);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Employee employee)
        {
            _db.Employees.Remove(employee);
            await _db.SaveChangesAsync();
        }

        public async Task DeletePermanentlyAsync(int employeeId)
        {
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeID == employeeId);
            if (employee == null)
            {
                return;
            }

            var relatedUsers = await _db.Users
                .Where(u => u.EmployeeID == employeeId)
                .ToListAsync();

            if (relatedUsers.Count > 0)
            {
                var relatedUserIds = relatedUsers.Select(u => u.UserID).ToList();

                var auditLogs = await _db.AuditLogs
                    .Where(log => log.UserID.HasValue && relatedUserIds.Contains(log.UserID.Value))
                    .ToListAsync();
                foreach (var log in auditLogs)
                {
                    log.UserID = null;
                }

                var approvedLeaves = await _db.LeaveRequests
                    .Where(l => l.ApprovedByUserID.HasValue && relatedUserIds.Contains(l.ApprovedByUserID.Value))
                    .ToListAsync();
                foreach (var leave in approvedLeaves)
                {
                    leave.ApprovedByUserID = null;
                }

                _db.Users.RemoveRange(relatedUsers);
            }

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

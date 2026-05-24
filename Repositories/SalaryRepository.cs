using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Repositories
{
    public class SalaryRepository
    {
        private readonly ApplicationDbContext _db;

        public SalaryRepository(ApplicationDbContext db) => _db = db;

        public async Task<Salary> GetByIdAsync(int id)
            => await _db.Salaries.Include(s => s.Employee).FirstOrDefaultAsync(s => s.SalaryID == id);

        public async Task<Salary> GetByEmployeeAndMonthAsync(int employeeId, int month, int year)
            => await _db.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.EmployeeID == employeeId && s.Month == month && s.Year == year);

        public async Task<IEnumerable<Salary>> GetByEmployeeAsync(int employeeId)
            => await _db.Salaries
                .Where(s => s.EmployeeID == employeeId)
                .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month)
                .ToListAsync();

        public async Task<IEnumerable<Salary>> GetByMonthAsync(int month, int year)
        {
            var list = await _db.Salaries
                .Include(s => s.Employee).ThenInclude(e => e.Department)
                .Where(s => s.Month == month && s.Year == year)
                .ToListAsync();

            return list
                .OrderBy(s => s.Employee?.Name ?? string.Empty)
                .ToList();
        }

        public async Task<Salary> GetLatestByEmployeeAsync(int employeeId)
            => await _db.Salaries
                .Where(s => s.EmployeeID == employeeId)
                .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month)
                .FirstOrDefaultAsync();

        public async Task AddAsync(Salary salary)
        {
            _db.Salaries.Add(salary);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Salary salary)
        {
            _db.Salaries.Update(salary);
            await _db.SaveChangesAsync();
        }

        public async Task UpsertAsync(Salary salary)
        {
            var existing = await GetByEmployeeAndMonthAsync(salary.EmployeeID, salary.Month, salary.Year);
            if (existing == null)
                _db.Salaries.Add(salary);
            else
            {
                existing.BaseSalary = salary.BaseSalary;
                existing.Allowances = salary.Allowances;
                existing.KPIBonus = salary.KPIBonus;
                existing.OTSalary = salary.OTSalary;
                existing.Commission = salary.Commission;
                existing.Bonus = salary.Bonus;
                existing.Deductions = salary.Deductions;
                existing.NetSalary = salary.NetSalary;
                existing.PaidDate = salary.PaidDate;
            }
            await _db.SaveChangesAsync();
        }
    }
}

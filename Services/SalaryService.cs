using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Repositories;

namespace QuanLyNhanSu_WPF.Services
{
    public class SalaryService
    {
        private readonly SalaryRepository _salaryRepo;
        private readonly AttendanceRepository _attendanceRepo;
        private const int StandardWorkDays = 26;

        public SalaryService(ApplicationDbContext db)
        {
            _salaryRepo = new SalaryRepository(db);
            _attendanceRepo = new AttendanceRepository(db);
        }

        /// <summary>
        /// Calculate monthly salary:
        /// NetSalary = (BaseSalary / 26) × WorkingDays + Allowances + Bonus - Deductions
        /// </summary>
        public async Task<Salary> CalculateMonthlySalaryAsync(
            Employee employee, int month, int year,
            decimal allowances = 0, decimal bonus = 0, decimal deductions = 0)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Calculate_Salary);

            decimal baseSalary = employee.Position?.BaseSalary ?? 0;

            // Count present/late days in month
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var records = await _attendanceRepo.GetByEmployeeAsync(employee.EmployeeID, from, to);
            int workingDays = records.Count(a => a.Status == "Present" || a.Status == "Late");

            decimal dailyRate = baseSalary / StandardWorkDays;
            decimal netSalary = dailyRate * workingDays + allowances + bonus - deductions;

            var salary = new Salary
            {
                EmployeeID = employee.EmployeeID,
                Month = month,
                Year = year,
                BaseSalary = baseSalary,
                Allowances = allowances,
                Bonus = bonus,
                Deductions = deductions,
                NetSalary = Math.Max(0, netSalary)
            };

            await _salaryRepo.UpsertAsync(salary);
            await AuditLogger.LogCreateAsync("Salaries", $"{employee.EmployeeID}/{month}/{year}", $"Net={netSalary:N0}");
            return salary;
        }

        public async Task<IEnumerable<Salary>> GetSalaryHistoryAsync(int employeeId)
        {
            var user = SessionManager.Instance.CurrentUser;
            // Employee can only see own salary
            if (user.Role == UserRole.Employee && user.EmployeeID != employeeId)
            {
                await AuditLogger.LogPermissionDeniedAsync(Permissions.View_All_Salaries);
                throw new UnauthorizedAccessException("Bạn không có quyền xem lương của nhân viên khác.");
            }
            return await _salaryRepo.GetByEmployeeAsync(employeeId);
        }

        public async Task<IEnumerable<Salary>> GetMonthlyPayrollAsync(int month, int year)
        {
            AuthorizationService.Current.CheckPermission(Permissions.View_All_Salaries);
            return await _salaryRepo.GetByMonthAsync(month, year);
        }

        public async Task<Salary> GetMyLatestSalaryAsync()
        {
            var user = SessionManager.Instance.CurrentUser;
            if (!user.EmployeeID.HasValue) return null;
            return await _salaryRepo.GetLatestByEmployeeAsync(user.EmployeeID.Value);
        }

        public async Task<Salary> GetLatestByEmployeeAsync(int employeeId)
            => await _salaryRepo.GetLatestByEmployeeAsync(employeeId);
    }
}

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

        public async Task<Salary> CalculateMonthlySalaryAsync(
            Employee employee, int month, int year,
            decimal allowances = 0, decimal kpiBonus = 0, decimal otHours = 0, decimal deductions = 0, decimal salesRevenue = 0)
        {
            if (employee == null)
                return null;

            AuthorizationService.Current.CheckPermission(Permissions.Calculate_Salary);

            var payrollPeriodEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            var tenureDays = Math.Max(0, (payrollPeriodEnd - employee.HireDate.Date).Days);
            var totalExpDays = employee.ExperienceDays + tenureDays;
            var yearsExp = totalExpDays / 365.0;
            var posName = employee.Position?.PosName?.ToLowerInvariant() ?? string.Empty;

            decimal baseSalary;
            if (posName.Contains("sale") || posName.Contains("kinh doanh"))
            {
                baseSalary = 8000000m;
            }
            else if (yearsExp < 0.5)
            {
                baseSalary = 4000000m;
            }
            else if (yearsExp < 1)
            {
                baseSalary = 9000000m;
            }
            else if (yearsExp < 4)
            {
                baseSalary = 13500000m;
            }
            else
            {
                baseSalary = 18500000m;
            }

            const decimal fixedAllowances = 1000000m;
            var factor = employee.EmploymentType switch
            {
                EmploymentType.Permanent => 1m,
                EmploymentType.Probation => 0.7m,
                EmploymentType.PartTime => 0.5m,
                _ => 1m
            };

            var adjustedBase = baseSalary * factor;
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var records = (await _attendanceRepo.GetByEmployeeAsync(employee.EmployeeID, from, to)).ToList();

            var workingDays = records.Count(a => AttendanceStatuses.IsPaidWorkingDay(a.Status));

            if (otHours == 0)
            {
                otHours = (decimal)records.Sum(a => a.OvertimeHours);
            }

            var hourlyRate = adjustedBase / StandardWorkDays / 8;
            var otSalary = otHours * hourlyRate * 1.5m;
            var commission = salesRevenue * (decimal)(employee.BaseCommissionRate / 100.0);
            var dailyRate = adjustedBase / StandardWorkDays;
            var netSalary = (dailyRate * workingDays) + kpiBonus + (allowances + fixedAllowances) + otSalary + commission - deductions;

            var salary = new Salary
            {
                EmployeeID = employee.EmployeeID,
                Month = month,
                Year = year,
                BaseSalary = adjustedBase,
                Allowances = allowances + fixedAllowances,
                KPIBonus = kpiBonus,
                OTSalary = otSalary,
                Commission = commission,
                Bonus = 0,
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
            if (!user.EmployeeID.HasValue)
                return null;

            return await _salaryRepo.GetLatestByEmployeeAsync(user.EmployeeID.Value);
        }

        public async Task<Salary> GetLatestByEmployeeAsync(int employeeId)
            => await _salaryRepo.GetLatestByEmployeeAsync(employeeId);
    }
}

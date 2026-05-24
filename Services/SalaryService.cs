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
        private readonly EmployeeRepository _employeeRepo;
        private const int StandardWorkDays = 26;
        private const decimal FixedAllowances = 1000000m;

        public SalaryService(ApplicationDbContext db)
        {
            _salaryRepo = new SalaryRepository(db);
            _attendanceRepo = new AttendanceRepository(db);
            _employeeRepo = new EmployeeRepository(db);
        }

        public async Task<Salary> CalculateMonthlySalaryAsync(
            Employee employee, int month, int year,
            decimal allowances = 0, decimal kpiBonus = 0, decimal otHours = 0, decimal deductions = 0, decimal salesRevenue = 0)
        {
            if (employee == null)
                return null;

            AuthorizationService.Current.CheckPermission(Permissions.Calculate_Salary);

            var adjustedBase = GetAdjustedBaseSalary(employee, month, year);
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var records = (await _attendanceRepo.GetByEmployeeAsync(employee.EmployeeID, from, to)).ToList();

            var workingDays = records.Sum(a => (decimal)AttendanceCalculations.CalculateDailyWorkUnits(a.Status, a.CheckIn, a.CheckOut));

            if (otHours == 0)
                otHours = (decimal)records.Sum(a => a.OvertimeHours);

            var hourlyRate = adjustedBase / StandardWorkDays / 8;
            var otSalary = otHours * hourlyRate * 1.5m;
            var commission = salesRevenue * (decimal)(employee.BaseCommissionRate / 100.0);
            var dailyRate = adjustedBase / StandardWorkDays;
            var netSalary = (dailyRate * workingDays) + kpiBonus + (allowances + FixedAllowances) + otSalary + commission - deductions;

            var salary = new Salary
            {
                EmployeeID = employee.EmployeeID,
                Month = month,
                Year = year,
                BaseSalary = adjustedBase,
                Allowances = allowances + FixedAllowances,
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

        public async Task<IEnumerable<DailySalaryEntry>> GetDailySalaryEntriesAsync(int month, int year, int? employeeId = null)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null)
                return new List<DailySalaryEntry>();

            var targetEmployeeId = ResolveEmployeeScope(user, employeeId);
            var employees = await GetEmployeesForScopeAsync(targetEmployeeId);
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var rows = new List<DailySalaryEntry>();

            foreach (var employee in employees)
            {
                var adjustedBase = GetAdjustedBaseSalary(employee, month, year);
                var dailyRate = adjustedBase / StandardWorkDays;
                var hourlyRate = dailyRate / 8;
                var records = await _attendanceRepo.GetByEmployeeAsync(employee.EmployeeID, from, to);

                foreach (var record in records.OrderBy(r => r.Date))
                {
                    var workUnits = AttendanceCalculations.CalculateDailyWorkUnits(record.Status, record.CheckIn, record.CheckOut);
                    var workingHours = AttendanceCalculations.CalculateWorkingHours(record.CheckIn, record.CheckOut);
                    var baseAmount = dailyRate * (decimal)workUnits;
                    var overtimeAmount = hourlyRate * (decimal)record.OvertimeHours * 1.5m;

                    rows.Add(new DailySalaryEntry
                    {
                        EmployeeID = employee.EmployeeID,
                        EmployeeCode = employee.Code,
                        EmployeeName = employee.Name,
                        DepartmentName = employee.Department?.DeptName,
                        Date = record.Date,
                        AttendanceStatus = record.Status,
                        WorkUnits = workUnits,
                        WorkingHours = workingHours,
                        OvertimeHours = record.OvertimeHours,
                        DailyRate = dailyRate,
                        BaseAmount = baseAmount,
                        OvertimeAmount = overtimeAmount,
                        TotalAmount = baseAmount + overtimeAmount,
                        Note = record.Note
                    });
                }
            }

            return rows
                .OrderBy(r => r.Date)
                .ThenBy(r => r.EmployeeName)
                .ToList();
        }

        public async Task<IEnumerable<MonthlySalarySummary>> GetMonthlySalarySummariesAsync(int month, int year, int? employeeId = null)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null)
                return new List<MonthlySalarySummary>();

            var targetEmployeeId = ResolveEmployeeScope(user, employeeId);
            var employees = await GetEmployeesForScopeAsync(targetEmployeeId);
            var dailyRows = (await GetDailySalaryEntriesAsync(month, year, targetEmployeeId)).ToList();
            var officialSalaries = (await _salaryRepo.GetByMonthAsync(month, year)).ToList();
            var summaries = new List<MonthlySalarySummary>();

            foreach (var employee in employees)
            {
                var rows = dailyRows.Where(r => r.EmployeeID == employee.EmployeeID).ToList();
                var official = officialSalaries.FirstOrDefault(s => s.EmployeeID == employee.EmployeeID);

                var summary = new MonthlySalarySummary
                {
                    EmployeeID = employee.EmployeeID,
                    EmployeeCode = employee.Code,
                    EmployeeName = employee.Name,
                    DepartmentName = employee.Department?.DeptName,
                    Month = month,
                    Year = year,
                    TotalWorkUnits = rows.Sum(r => r.WorkUnits),
                    TotalWorkingHours = rows.Sum(r => r.WorkingHours),
                    TotalOvertimeHours = rows.Sum(r => r.OvertimeHours),
                    DailySalaryTotal = rows.Sum(r => r.BaseAmount),
                    OvertimeSalaryTotal = rows.Sum(r => r.OvertimeAmount),
                    FixedAllowances = FixedAllowances,
                    ProjectedNetSalary = rows.Sum(r => r.TotalAmount) + FixedAllowances,
                    OfficialNetSalary = official?.NetSalary,
                    OfficialBaseSalary = official?.BaseSalary
                };

                summaries.Add(summary);
            }

            return summaries.OrderBy(s => s.EmployeeName).ToList();
        }

        public async Task<IEnumerable<Salary>> GetSalaryHistoryAsync(int employeeId)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null)
                return new List<Salary>();

            if (user.Role == UserRole.Employee && user.EmployeeID != employeeId)
            {
                await AuditLogger.LogPermissionDeniedAsync(Permissions.View_All_Salaries);
                throw new UnauthorizedAccessException("Ban khong co quyen xem luong cua nhan vien khac.");
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
            if (user == null || !user.EmployeeID.HasValue)
                return null;

            return await _salaryRepo.GetLatestByEmployeeAsync(user.EmployeeID.Value);
        }

        public async Task<Salary> GetLatestByEmployeeAsync(int employeeId)
            => await _salaryRepo.GetLatestByEmployeeAsync(employeeId);

        private async Task<List<Employee>> GetEmployeesForScopeAsync(int? employeeId)
        {
            if (employeeId.HasValue)
            {
                var employee = await _employeeRepo.GetByIdAsync(employeeId.Value);
                return employee == null ? new List<Employee>() : new List<Employee> { employee };
            }

            return (await _employeeRepo.GetActiveAsync()).ToList();
        }

        private static int? ResolveEmployeeScope(User user, int? requestedEmployeeId)
        {
            if (user.Role == UserRole.Employee)
                return user.EmployeeID;

            return requestedEmployeeId;
        }

        private decimal GetAdjustedBaseSalary(Employee employee, int month, int year)
        {
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

            var factor = employee.EmploymentType switch
            {
                EmploymentType.Permanent => 1m,
                EmploymentType.Probation => 0.7m,
                EmploymentType.PartTime => 0.5m,
                _ => 1m
            };

            return baseSalary * factor;
        }
    }
}

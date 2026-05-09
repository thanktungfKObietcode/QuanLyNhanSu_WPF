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
            decimal allowances = 0, decimal kpiBonus = 0, decimal otHours = 0, decimal deductions = 0, decimal salesRevenue = 0)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Calculate_Salary);

            // 1. Xác định lương cứng dựa trên vị trí và kinh nghiệm (Yêu cầu công ty truyền thông)
            decimal baseSalary = 0;
            
            // Tính tổng kinh nghiệm = Kinh nghiệm cũ (ngày) + Thâm niên tại công ty (ngày)
            int tenureDays = (DateTime.Today - employee.HireDate).Days;
            double totalExpDays = employee.ExperienceDays + Math.Max(0, tenureDays);
            double yearsExp = totalExpDays / 365.0;
            string posName = employee.Position?.PosName?.ToLower() ?? "";

            if (posName.Contains("sale") || posName.Contains("kinh doanh"))
            {
                baseSalary = 8000000m; // Sale Media lương cứng thấp hơn nhưng commission cao
            }
            else if (yearsExp < 0.5) // Intern / Thực tập
            {
                baseSalary = 4000000m;
            }
            else if (yearsExp < 1) // Nhân viên mới
            {
                baseSalary = 9000000m;
            }
            else if (yearsExp < 4) // Chuyên viên / Junior
            {
                baseSalary = 13500000m;
            }
            else // Leader / Manager / Senior
            {
                baseSalary = 18500000m;
            }

            // 2. Tính phụ cấp cố định (ăn trưa, gửi xe...) - Giả định 1.000.000 VNĐ cho công ty truyền thông
            decimal fixedAllowances = 1000000m;

            // Apply employment type factor (Thử việc 70%, v.v.)
            decimal factor = employee.EmploymentType switch
            {
                EmploymentType.Permanent => 1m,
                EmploymentType.Probation => 0.7m,
                EmploymentType.PartTime => 0.5m,
                _ => 1m
            };
            
            decimal adjustedBase = baseSalary * factor;
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var records = await _attendanceRepo.GetByEmployeeAsync(employee.EmployeeID, from, to);
            // Ngày công thực tế bao gồm Có mặt, Đi muộn và Nghỉ phép (có lương)
            int workingDays = records.Count(a => a.Status == "Có mặt" || a.Status == "Đi muộn" || a.Status == "Nghỉ phép");
            
            // Tự động lấy tổng giờ OT từ bảng điểm danh nếu không truyền vào thủ công
            if (otHours == 0)
            {
                otHours = (decimal)records.Sum(a => a.OvertimeHours);
            }

            // 3. Tính lương OT (Làm thêm giờ) - Sau khi đã có tổng giờ OT
            decimal hourlyRate = baseSalary / StandardWorkDays / 8;
            decimal otSalary = otHours * hourlyRate * 1.5m;

            // 4. Tính hoa hồng Sales (Chuyên nghiệp: Tự động tính dựa trên doanh số và tỷ lệ thiết lập)
            decimal commission = salesRevenue * (decimal)(employee.BaseCommissionRate / 100.0);

            decimal dailyRate = adjustedBase / StandardWorkDays;
            
            // Tổng thu nhập = Lương cứng (theo ngày công) + KPI + Phụ cấp + OT + Hoa hồng - Khấu trừ
            decimal netSalary = (dailyRate * workingDays) + kpiBonus + (allowances + fixedAllowances) + otSalary + commission - deductions;

            var salary = new Salary
            {
                EmployeeID = employee.EmployeeID,
                Month = month,
                Year = year,
                BaseSalary = baseSalary,
                Allowances = allowances + fixedAllowances,
                KPIBonus = kpiBonus,
                OTSalary = otSalary,
                Commission = commission,
                Bonus = 0, // Dùng KPIBonus thay thế hoặc gộp chung
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

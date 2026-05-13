using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Repositories;

namespace QuanLyNhanSu_WPF.Services
{
    public class EmployeeService
    {
        private readonly EmployeeRepository _empRepo;
        private readonly UserRepository _userRepo;

        public EmployeeService(ApplicationDbContext db)
        {
            _empRepo = new EmployeeRepository(db);
            _userRepo = new UserRepository(db);
        }

        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            AuthorizationService.Current.CheckPermission(Permissions.View_All_Employees);
            return await _empRepo.GetAllAsync();
        }

        public async Task<IEnumerable<Employee>> SearchAsync(string keyword)
        {
            AuthorizationService.Current.CheckPermission(Permissions.View_All_Employees);
            return await _empRepo.SearchAsync(keyword);
        }

        public async Task<Employee> GetByIdAsync(int id)
            => await _empRepo.GetByIdAsync(id);

        public async Task<(bool Success, string Error)> AddEmployeeAsync(Employee employee)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Add_Employee);

            if (string.IsNullOrWhiteSpace(employee.Name))
                return (false, "Tên nhân viên không được để trống.");

            if (string.IsNullOrWhiteSpace(employee.Code))
                return (false, "Mã nhân viên không được để trống.");

            employee.Name = employee.Name.Trim();
            employee.Code = employee.Code.Trim().ToUpper();

            if (await _empRepo.ExistsByCodeAsync(employee.Code))
                return (false, $"Mã nhân viên '{employee.Code}' đã tồn tại.");

            await _empRepo.AddAsync(employee);
            await AuditLogger.LogCreateAsync("Employees", employee.EmployeeID.ToString(), employee.Name);
            return (true, null);
        }

        public async Task<(bool Success, string Error)> UpdateEmployeeAsync(Employee employee)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Edit_Employee);

            if (string.IsNullOrWhiteSpace(employee.Name))
                return (false, "Tên nhân viên không được để trống.");

            if (string.IsNullOrWhiteSpace(employee.Code))
                return (false, "Mã nhân viên không được để trống.");

            employee.Name = employee.Name.Trim();
            employee.Code = employee.Code.Trim().ToUpper();

            if (await _empRepo.ExistsByCodeAsync(employee.Code, employee.EmployeeID))
                return (false, $"Mã nhân viên '{employee.Code}' đã được dùng bởi nhân viên khác.");

            await _empRepo.UpdateAsync(employee);
            await AuditLogger.LogUpdateAsync("Employees", employee.EmployeeID.ToString(), null, employee.Name);
            return (true, null);
        }

        public async Task<bool> DeactivateAsync(int employeeId)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Delete_Employee);
            await _empRepo.DeactivateAsync(employeeId);
            await AuditLogger.LogUpdateAsync("Employees", employeeId.ToString(), "Active", "Inactive");
            return true;
        }

        public async Task<(bool Success, string Error, string GeneratedPassword)> CreateUserAccountAsync(int employeeId)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Create_User_Account);

            var employee = await _empRepo.GetByIdAsync(employeeId);
            if (employee == null)
                return (false, "Không tìm thấy nhân viên.", null);

            var existing = await _userRepo.GetByEmployeeIdAsync(employeeId);
            if (existing != null)
                return (false, "Nhân viên này đã có tài khoản.", null);

            var generatedPwd = employee.Code + "@Abc1";
            PasswordHasher.HashPassword(generatedPwd, out var hash, out var salt);

            var user = new User
            {
                Username = employee.Code.ToLower(),
                PasswordHash = hash,
                Salt = salt,
                EmployeeID = employeeId,
                Role = UserRole.Employee,
                IsActive = true
            };

            await _userRepo.AddAsync(user);
            await AuditLogger.LogCreateAsync("Users", user.UserID.ToString(), $"Account for emp {employeeId}");
            return (true, null, generatedPwd);
        }

        public string SavePhoto(string sourceFilePath, int employeeId)
        {
            try
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_data", "photos");
                Directory.CreateDirectory(dir);
                var ext = Path.GetExtension(sourceFilePath);
                var dest = Path.Combine(dir, $"emp_{employeeId}{ext}");
                File.Copy(sourceFilePath, dest, overwrite: true);
                return dest;
            }
            catch
            {
                return null;
            }
        }
    }
}

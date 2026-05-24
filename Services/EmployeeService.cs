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

            var validationError = await ValidateEmployeeAsync(employee, isUpdate: false);
            if (validationError != null)
            {
                return (false, validationError);
            }

            if (await _empRepo.ExistsByCodeAsync(employee.Code))
            {
                return (false, $"Ma nhan vien '{employee.Code}' da ton tai.");
            }

            await _empRepo.AddAsync(employee);
            await AuditLogger.LogCreateAsync("Employees", employee.EmployeeID.ToString(), employee.Name);
            return (true, null);
        }

        public async Task<(bool Success, string Error)> UpdateEmployeeAsync(Employee employee)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Edit_Employee);

            var validationError = await ValidateEmployeeAsync(employee, isUpdate: true);
            if (validationError != null)
            {
                return (false, validationError);
            }

            if (await _empRepo.ExistsByCodeAsync(employee.Code, employee.EmployeeID))
            {
                return (false, $"Ma nhan vien '{employee.Code}' da duoc dung boi nhan vien khac.");
            }

            await _empRepo.UpdateAsync(employee);
            await AuditLogger.LogUpdateAsync("Employees", employee.EmployeeID.ToString(), null, employee.Name);
            return (true, null);
        }

        public async Task<bool> DeactivateAsync(int employeeId)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Delete_Employee);

            var currentUser = SessionManager.Instance.CurrentUser;
            if (currentUser?.EmployeeID == employeeId)
            {
                throw new InvalidOperationException("Khong the xoa chinh nhan vien dang dang nhap.");
            }

            var employee = await _empRepo.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Khong tim thay nhan vien can xoa.");
            }

            var photoPath = employee.Photo;
            var employeeName = employee.Name;

            await _empRepo.DeletePermanentlyAsync(employeeId);
            TryDeletePhoto(photoPath);
            await AuditLogger.LogDeleteAsync("Employees", employeeId.ToString(), employeeName);
            return true;
        }

        public async Task<(bool Success, string Error, string GeneratedPassword)> CreateUserAccountAsync(int employeeId)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Create_User_Account);

            var employee = await _empRepo.GetByIdAsync(employeeId);
            if (employee == null)
            {
                return (false, "Khong tim thay nhan vien.", null);
            }

            var existing = await _userRepo.GetByEmployeeIdAsync(employeeId);
            if (existing != null)
            {
                return (false, "Nhan vien nay da co tai khoan.", null);
            }

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

        private async Task<string> ValidateEmployeeAsync(Employee employee, bool isUpdate)
        {
            if (employee == null)
            {
                return "Du lieu nhan vien khong hop le.";
            }

            employee.Name = employee.Name?.Trim();
            employee.Code = employee.Code?.Trim().ToUpperInvariant();
            employee.Gender = NormalizeOptionalText(employee.Gender);
            employee.PhoneNumber = NormalizeOptionalText(employee.PhoneNumber);
            employee.Email = NormalizeOptionalText(employee.Email);
            employee.Address = NormalizeOptionalText(employee.Address);
            employee.Degree = NormalizeOptionalText(employee.Degree);
            employee.Photo = NormalizeOptionalText(employee.Photo);

            if (string.IsNullOrWhiteSpace(employee.Name))
            {
                return "Ten nhan vien khong duoc de trong.";
            }

            if (string.IsNullOrWhiteSpace(employee.Code))
            {
                return "Ma nhan vien khong duoc de trong.";
            }

            if (isUpdate && employee.EmployeeID <= 0)
            {
                return "Khong xac dinh duoc nhan vien can cap nhat.";
            }

            if (employee.Code.Length > 20)
            {
                return "Ma nhan vien khong duoc vuot qua 20 ky tu.";
            }

            if (employee.Name.Length > 200)
            {
                return "Ho va ten khong duoc vuot qua 200 ky tu.";
            }

            if (!string.IsNullOrEmpty(employee.Gender) && employee.Gender.Length > 10)
            {
                return "Gioi tinh khong duoc vuot qua 10 ky tu.";
            }

            if (!string.IsNullOrEmpty(employee.PhoneNumber) && employee.PhoneNumber.Length > 20)
            {
                return "So dien thoai khong duoc vuot qua 20 ky tu.";
            }

            if (!string.IsNullOrEmpty(employee.Email) && employee.Email.Length > 200)
            {
                return "Email khong duoc vuot qua 200 ky tu.";
            }

            if (!string.IsNullOrEmpty(employee.Address) && employee.Address.Length > 500)
            {
                return "Dia chi khong duoc vuot qua 500 ky tu.";
            }

            if (!string.IsNullOrEmpty(employee.Photo) && employee.Photo.Length > 500)
            {
                return "Duong dan anh khong duoc vuot qua 500 ky tu.";
            }

            if (employee.DepartmentID <= 0)
            {
                employee.DepartmentID = null;
            }

            if (employee.PositionID <= 0)
            {
                employee.PositionID = null;
            }

            employee.Department = null;
            employee.Position = null;

            if (employee.DepartmentID.HasValue && !await _empRepo.DepartmentExistsAsync(employee.DepartmentID.Value))
            {
                return "Phong ban duoc chon khong con ton tai.";
            }

            if (employee.PositionID.HasValue && !await _empRepo.PositionExistsAsync(employee.PositionID.Value))
            {
                return "Chuc vu duoc chon khong con ton tai.";
            }

            return null;
        }

        private static string NormalizeOptionalText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static void TryDeletePhoto(string photoPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(photoPath) && File.Exists(photoPath))
                {
                    File.Delete(photoPath);
                }
            }
            catch
            {
            }
        }
    }
}

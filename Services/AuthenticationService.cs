using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Repositories;

namespace QuanLyNhanSu_WPF.Services
{
    public class AuthenticationResult
    {
        public bool Succeeded { get; set; }
        public string ErrorMessage { get; set; }
        public User User { get; set; }
    }

    public class AccountRecoveryResult
    {
        public bool Succeeded { get; set; }
        public string ErrorMessage { get; set; }
        public string Username { get; set; }
        public string TemporaryPassword { get; set; }
        public string Message { get; set; }
    }

    public class AuthenticationService
    {
        private readonly UserRepository _userRepo;
        private readonly ApplicationDbContext _db;
        private readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(30);
        private const int MaxFailedAttempts = 5;
        private const string DefaultAdminUsername = "admin";
        private const string DefaultAdminPassword = "Admin@123";
        private const string DefaultAdminRecoveryKey = "MEDIA-HR-ADMIN-2026";

        public AuthenticationService(ApplicationDbContext db)
        {
            _db = db;
            _userRepo = new UserRepository(db);
        }

        public async Task<AuthenticationResult> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new AuthenticationResult { Succeeded = false, ErrorMessage = "Username or password is empty." };

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                return new AuthenticationResult { Succeeded = false, ErrorMessage = "Invalid username or password." };

            if (!user.IsActive)
                return new AuthenticationResult { Succeeded = false, ErrorMessage = "Account is disabled." };

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                return new AuthenticationResult { Succeeded = false, ErrorMessage = $"Account is locked until {user.LockoutEnd.Value:HH:mm:ss}." };

            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash, user.Salt))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.Add(_lockoutDuration);
                    user.FailedLoginAttempts = 0;
                }
                await _userRepo.UpdateAsync(user);
                return new AuthenticationResult { Succeeded = false, ErrorMessage = "Invalid username or password." };
            }

            // success
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLogin = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);

            SessionManager.Instance.StartSession(user);

            return new AuthenticationResult { Succeeded = true, User = user };
        }

        public Task LogoutAsync()
        {
            SessionManager.Instance.EndSession();
            return Task.CompletedTask;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;
            if (!PasswordHasher.VerifyPassword(oldPassword, user.PasswordHash, user.Salt)) return false;
            PasswordHasher.HashPassword(newPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.Salt = salt;
            await _userRepo.UpdateAsync(user);
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string username, string newPassword)
        {
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null) return false;
            PasswordHasher.HashPassword(newPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.Salt = salt;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _userRepo.UpdateAsync(user);
            return true;
        }

        public async Task<bool> LockAccountAsync(string username)
        {
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null) return false;
            user.LockoutEnd = DateTime.UtcNow.Add(_lockoutDuration);
            user.FailedLoginAttempts = 0;
            await _userRepo.UpdateAsync(user);
            return true;
        }

        public async Task<bool> UnlockAccountAsync(string username)
        {
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null) return false;
            user.LockoutEnd = null;
            user.FailedLoginAttempts = 0;
            await _userRepo.UpdateAsync(user);
            return true;
        }

        public async Task<AccountRecoveryResult> RecoverEmployeeAccessAsync(string employeeCode, string contactValue)
        {
            var normalizedCode = employeeCode?.Trim().ToUpperInvariant();
            var normalizedContact = contactValue?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedCode) || string.IsNullOrWhiteSpace(normalizedContact))
            {
                return new AccountRecoveryResult
                {
                    Succeeded = false,
                    ErrorMessage = "Can nhap ma nhan vien va so dien thoai hoac email."
                };
            }

            var employee = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.Code.ToUpper() == normalizedCode &&
                    ((e.PhoneNumber != null && e.PhoneNumber == normalizedContact) ||
                     (e.Email != null && e.Email.ToLower() == normalizedContact.ToLower())));

            if (employee == null)
            {
                return new AccountRecoveryResult
                {
                    Succeeded = false,
                    ErrorMessage = "Khong tim thay nhan vien khop voi thong tin xac minh."
                };
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.EmployeeID == employee.EmployeeID);
            if (user == null)
            {
                return new AccountRecoveryResult
                {
                    Succeeded = false,
                    ErrorMessage = "Nhan vien nay chua duoc cap tai khoan dang nhap."
                };
            }

            var temporaryPassword = normalizedCode + "@Abc1";
            ApplyPassword(user, temporaryPassword);
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _userRepo.UpdateAsync(user);

            return new AccountRecoveryResult
            {
                Succeeded = true,
                Username = user.Username,
                TemporaryPassword = temporaryPassword,
                Message = $"Da khoi phuc tai khoan cho {employee.Name}.\nTen dang nhap: {user.Username}\nMat khau tam thoi: {temporaryPassword}"
            };
        }

        public async Task<AccountRecoveryResult> RecoverAdminAccessAsync(string recoveryKey)
        {
            if (string.IsNullOrWhiteSpace(recoveryKey))
            {
                return new AccountRecoveryResult
                {
                    Succeeded = false,
                    ErrorMessage = "Can nhap ma cuu ho quan tri."
                };
            }

            if (!string.Equals(recoveryKey.Trim(), GetAdminRecoveryKey(), StringComparison.Ordinal))
            {
                return new AccountRecoveryResult
                {
                    Succeeded = false,
                    ErrorMessage = "Ma cuu ho quan tri khong dung."
                };
            }

            var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == DefaultAdminUsername || u.Role == UserRole.Admin);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Username = DefaultAdminUsername,
                    Role = UserRole.Admin,
                    IsActive = true
                };
                ApplyPassword(adminUser, DefaultAdminPassword);
                adminUser.FailedLoginAttempts = 0;
                adminUser.LockoutEnd = null;
                await _userRepo.AddAsync(adminUser);
            }
            else
            {
                adminUser.Username = DefaultAdminUsername;
                adminUser.Role = UserRole.Admin;
                adminUser.IsActive = true;
                ApplyPassword(adminUser, DefaultAdminPassword);
                adminUser.FailedLoginAttempts = 0;
                adminUser.LockoutEnd = null;
                await _userRepo.UpdateAsync(adminUser);
            }

            return new AccountRecoveryResult
            {
                Succeeded = true,
                Username = DefaultAdminUsername,
                TemporaryPassword = DefaultAdminPassword,
                Message = $"Da khoi phuc tai khoan quan tri.\nTen dang nhap: {DefaultAdminUsername}\nMat khau tam thoi: {DefaultAdminPassword}"
            };
        }

        private static string GetAdminRecoveryKey()
            => Environment.GetEnvironmentVariable("MEDIA_HR_ADMIN_RECOVERY_KEY")?.Trim() ?? DefaultAdminRecoveryKey;

        private static void ApplyPassword(User user, string rawPassword)
        {
            PasswordHasher.HashPassword(rawPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.Salt = salt;
        }
    }
}

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

    public class AuthenticationService
    {
        private readonly UserRepository _userRepo;
        private readonly ApplicationDbContext _db;
        private readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(30);
        private const int MaxFailedAttempts = 5;

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
    }
}

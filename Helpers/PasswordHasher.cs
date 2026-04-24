using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace QuanLyNhanSu_WPF.Helpers
{
    public static class PasswordHasher
    {
        public static void HashPassword(string password, out string hash, out string salt)
        {
            using var rng = RandomNumberGenerator.Create();
            var saltBytes = new byte[16];
            rng.GetBytes(saltBytes);
            salt = Convert.ToBase64String(saltBytes);

            using var sha256 = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(password + salt);
            var hashBytes = sha256.ComputeHash(combined);
            hash = Convert.ToBase64String(hashBytes);
        }

        public static bool VerifyPassword(string password, string hash, string salt)
        {
            using var sha256 = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(password + salt);
            var hashBytes = sha256.ComputeHash(combined);
            var computedHash = Convert.ToBase64String(hashBytes);
            return computedHash == hash;
        }

        /// <summary>
        /// Validates password policy: min 8 chars, at least 1 uppercase, 1 lowercase, 1 digit.
        /// Returns null if valid, or an error message string.
        /// </summary>
        public static string ValidatePasswordPolicy(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return "Mật khẩu phải có ít nhất 8 ký tự.";
            if (!Regex.IsMatch(password, "[A-Z]"))
                return "Mật khẩu phải có ít nhất 1 chữ hoa.";
            if (!Regex.IsMatch(password, "[a-z]"))
                return "Mật khẩu phải có ít nhất 1 chữ thường.";
            if (!Regex.IsMatch(password, "[0-9]"))
                return "Mật khẩu phải có ít nhất 1 chữ số.";
            return null;
        }
    }
}

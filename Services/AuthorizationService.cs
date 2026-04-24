using System;
using System.Collections.Generic;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Services
{
    public class AuthorizationService
    {
        public static AuthorizationService Current { get; } = new AuthorizationService();
        private AuthorizationService() { }

        public bool HasPermission(string permission)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null) return false;
            if (RolePermissions.PermissionsByRole.TryGetValue(user.Role, out var perms))
                return perms.Contains(permission);
            return false;
        }

        public void CheckPermission(string permission)
        {
            if (!HasPermission(permission))
                throw new UnauthorizedAccessException($"Bạn không có quyền truy cập chức năng này: {permission}");
        }

        public bool CanAccessModule(string moduleName)
        {
            // Example: map moduleName to permission
            return HasPermission(moduleName);
        }

        public UserRole? GetUserRole()
        {
            return SessionManager.Instance.CurrentUser?.Role;
        }
    }
}

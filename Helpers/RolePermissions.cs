using System.Collections.Generic;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Helpers
{
    public static class RolePermissions
    {
        public static readonly Dictionary<UserRole, List<string>> PermissionsByRole = new()
        {
            [UserRole.Admin] = new List<string>
            {
                Permissions.View_All_Employees,
                Permissions.Add_Employee,
                Permissions.Edit_Employee,
                Permissions.Delete_Employee,
                Permissions.Manage_Employees,
                Permissions.Create_User_Account,
                Permissions.Manage_Departments,
                Permissions.View_All_Attendance,
                Permissions.Manage_Attendance,
                Permissions.View_Self_Attendance,
                Permissions.Approve_Leave,
                Permissions.View_All_Leaves,
                Permissions.Request_Leave,
                Permissions.View_All_Salaries,
                Permissions.Calculate_Salary,
                Permissions.Export_Data,
                Permissions.View_Self_Salary,
                Permissions.Edit_Self_Info,
            },
            [UserRole.Employee] = new List<string>
            {
                Permissions.View_Self_Attendance,
                Permissions.Request_Leave,
                Permissions.View_Self_Salary,
                Permissions.Edit_Self_Info,
            }
        };
    }
}

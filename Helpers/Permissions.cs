namespace QuanLyNhanSu_WPF.Helpers
{
    public static class Permissions
    {
        // Employee
        public const string View_All_Employees    = "View_All_Employees";
        public const string Add_Employee          = "Add_Employee";
        public const string Edit_Employee         = "Edit_Employee";
        public const string Delete_Employee       = "Delete_Employee";
        public const string Manage_Employees      = "Manage_Employees";
        public const string Create_User_Account   = "Create_User_Account";

        // Department / Position
        public const string Manage_Departments    = "Manage_Departments";

        // Attendance
        public const string View_All_Attendance   = "View_All_Attendance";
        public const string Manage_Attendance     = "Manage_Attendance";
        public const string View_Self_Attendance  = "View_Self_Attendance";

        // Leave
        public const string Approve_Leave         = "Approve_Leave";
        public const string View_All_Leaves       = "View_All_Leaves";
        public const string Request_Leave         = "Request_Leave";

        // Salary
        public const string View_All_Salaries     = "View_All_Salaries";
        public const string Calculate_Salary      = "Calculate_Salary";
        public const string Export_Data           = "Export_Data";
        public const string View_Self_Salary      = "View_Self_Salary";

        // Profile
        public const string Edit_Self_Info        = "Edit_Self_Info";
    }
}

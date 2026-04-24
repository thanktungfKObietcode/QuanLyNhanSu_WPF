using System;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.ViewModels;

namespace QuanLyNhanSu_WPF.Helpers
{
    public class ViewModelFactory
    {
        private readonly ApplicationDbContext _db;

        public ViewModelFactory()
        {
            _db = new ApplicationDbContext(DbContextFactory.CreateOptions());
        }

        public BaseViewModel CreateViewModel(string viewKey)
        {
            return viewKey switch
            {
                "AdminDashboard" => new AdminDashboardViewModel(),
                "EmployeeDashboard" => new EmployeeDashboardViewModel(),
                "EmployeeManagement" => new EmployeeManagementViewModel(),
                "DepartmentPosition" => new DepartmentPositionViewModel(),
                "Attendance" => new AttendanceViewModel(),
                "MyAttendance" => new AttendanceViewModel(), // Reuses same VM, logic handles role
                "LeaveRequest" => new LeaveRequestViewModel(),
                "LeaveApproval" => new LeaveRequestViewModel(), // Reuses same VM
                "SalaryManagement" => new SalaryViewModel(),
                "MySalary" => new SalaryViewModel(),
                _ => throw new ArgumentException($"Unknown view key: {viewKey}")
            };
        }
    }
}

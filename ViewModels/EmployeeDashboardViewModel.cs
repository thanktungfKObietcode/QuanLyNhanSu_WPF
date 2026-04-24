using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Repositories;
using QuanLyNhanSu_WPF.Services;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class EmployeeDashboardViewModel : BaseViewModel
    {
        private Employee _employeeInfo;
        private int _workingDaysThisMonth;
        private int _lateDaysThisMonth;
        private int _leaveDaysRemaining;
        private Salary _lastSalary;
        private bool _isLoading;

        public Employee EmployeeInfo { get => _employeeInfo; set => SetProperty(ref _employeeInfo, value); }
        public int WorkingDaysThisMonth { get => _workingDaysThisMonth; set => SetProperty(ref _workingDaysThisMonth, value); }
        public int LateDaysThisMonth { get => _lateDaysThisMonth; set => SetProperty(ref _lateDaysThisMonth, value); }
        public int LeaveDaysRemaining { get => _leaveDaysRemaining; set => SetProperty(ref _leaveDaysRemaining, value); }
        public Salary LastSalary { get => _lastSalary; set => SetProperty(ref _lastSalary, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        public ObservableCollection<LeaveRequest> RecentLeaveRequests { get; } = new();
        public ObservableCollection<Attendance> ThisMonthAttendance { get; } = new();

        public ICommand RefreshCommand { get; }
        public event Action RequestLeaveRequested;
        public event Action ViewAttendanceRequested;

        public EmployeeDashboardViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await LoadPersonalData());
            Task.Run(LoadPersonalData);
        }

        public async Task LoadPersonalData()
        {
            IsLoading = true;
            try
            {
                var user = SessionManager.Instance.CurrentUser;
                if (user?.EmployeeID == null) return;

                var options = DbContextFactory.CreateOptions();
                await using var db = new ApplicationDbContext(options);

                var empRepo = new EmployeeRepository(db);
                var attRepo = new AttendanceRepository(db);
                var leaveRepo = new LeaveRequestRepository(db);
                var salaryRepo = new SalaryRepository(db);
                var leaveService = new LeaveRequestService(db);

                var emp = await empRepo.GetByIdAsync(user.EmployeeID.Value);
                EmployeeInfo = emp;

                // This month attendance
                var from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var to = DateTime.Today;
                var attendance = await attRepo.GetByEmployeeAsync(user.EmployeeID.Value, from, to);

                int workingDays = 0, lateDays = 0;
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    ThisMonthAttendance.Clear();
                    foreach (var a in attendance)
                    {
                        ThisMonthAttendance.Add(a);
                        if (a.Status == "Present") workingDays++;
                        else if (a.Status == "Late") { workingDays++; lateDays++; }
                    }
                });
                WorkingDaysThisMonth = workingDays;
                LateDaysThisMonth = lateDays;

                // Remaining leave
                LeaveDaysRemaining = await leaveService.CalculateRemainingLeavesAsync(user.EmployeeID.Value);

                // Recent leave requests (last 5)
                var leaves = await leaveRepo.GetByEmployeeAsync(user.EmployeeID.Value);
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    RecentLeaveRequests.Clear();
                    int count = 0;
                    foreach (var l in leaves)
                    {
                        if (count++ >= 5) break;
                        RecentLeaveRequests.Add(l);
                    }
                });

                // Last salary
                LastSalary = await salaryRepo.GetLatestByEmployeeAsync(user.EmployeeID.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmployeeDashboard error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void OnRequestLeave() => RequestLeaveRequested?.Invoke();
        public void OnViewAttendance() => ViewAttendanceRequested?.Invoke();
    }
}

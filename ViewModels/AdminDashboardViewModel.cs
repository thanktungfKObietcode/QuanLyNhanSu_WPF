using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Repositories;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class AdminDashboardViewModel : BaseViewModel
    {
        private int _totalEmployees;
        private int _totalDepartments;
        private int _pendingLeaveRequests;
        private int _employeesPresent;
        private int _employeesOnLeave;
        private double _monthlyAttendanceRate;
        private bool _isLoading;
        private string[] _attendanceTrendLabels;
        private string[] _leaveStatusLabels;
        private DateTime _lastUpdated;
        private readonly System.Timers.Timer _autoRefreshTimer;

        public int TotalEmployees { get => _totalEmployees; set => SetProperty(ref _totalEmployees, value); }
        public int TotalDepartments { get => _totalDepartments; set => SetProperty(ref _totalDepartments, value); }
        public int PendingLeaveRequests { get => _pendingLeaveRequests; set => SetProperty(ref _pendingLeaveRequests, value); }
        public int EmployeesPresent { get => _employeesPresent; set => SetProperty(ref _employeesPresent, value); }
        public int EmployeesOnLeave { get => _employeesOnLeave; set => SetProperty(ref _employeesOnLeave, value); }
        public double MonthlyAttendanceRate { get => _monthlyAttendanceRate; set => SetProperty(ref _monthlyAttendanceRate, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public Func<double, string> YFormatter { get; set; } = value => value.ToString("N0") + " ₫";
        public string[] AttendanceTrendLabels { get => _attendanceTrendLabels; set => SetProperty(ref _attendanceTrendLabels, value); }
        public string[] LeaveStatusLabels { get => _leaveStatusLabels; set => SetProperty(ref _leaveStatusLabels, value); }
        public DateTime LastUpdated { get => _lastUpdated; set => SetProperty(ref _lastUpdated, value); }

        public ObservableCollection<AuditLog> RecentActivities { get; } = new();
        public ObservableCollection<Employee> UpcomingBirthdays { get; } = new();
        public ObservableCollection<Employee> UpcomingAnniversaries { get; } = new();
        public SeriesCollection AttendanceTrendSeries { get; } = new();
        public SeriesCollection DepartmentDistributionSeries { get; } = new();
        public SeriesCollection BudgetDistributionSeries { get; } = new();
        public SeriesCollection LeaveStatusSeries { get; } = new();

        public ICommand RefreshCommand { get; }

        public event Action NavigateToEmployees;
        public event Action NavigateToDepartments;
        public event Action NavigateToLeaves;
        public event Action NavigateToAttendance;

        public AdminDashboardViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await LoadStatistics());
            AttendanceTrendLabels = Array.Empty<string>();
            LeaveStatusLabels = Array.Empty<string>();

            _autoRefreshTimer = new System.Timers.Timer(5 * 60 * 1000);
            _autoRefreshTimer.Elapsed += async (_, _) => await LoadStatistics();
            _autoRefreshTimer.AutoReset = true;
            _autoRefreshTimer.Start();

            Task.Run(LoadStatistics);
        }

        public async Task LoadStatistics()
        {
            IsLoading = true;
            LastUpdated = DateTime.Now;

            try
            {
                var options = DbContextFactory.CreateOptions();
                await using var db = new ApplicationDbContext(options);

                var empRepo = new EmployeeRepository(db);
                var attRepo = new AttendanceRepository(db);
                var leaveRepo = new LeaveRequestRepository(db);
                var today = DateTime.Today;

                TotalEmployees = await empRepo.GetActiveCountAsync();
                TotalDepartments = await db.Departments.CountAsync();
                PendingLeaveRequests = await leaveRepo.GetPendingCountAsync();
                EmployeesPresent = await attRepo.GetPresentTodayCountAsync();
                EmployeesOnLeave = await attRepo.GetOnLeaveCountAsync();
                MonthlyAttendanceRate = await attRepo.GetMonthlyAttendanceRateAsync(today.Month, today.Year);

                var logs = await db.AuditLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(10)
                    .ToListAsync();

                var birthdays = await empRepo.GetBirthdaysThisMonthAsync();
                var anniversaries = await db.Employees
                    .Where(e => e.Status == EmployeeStatus.Active && e.HireDate.Month == today.Month)
                    .OrderBy(e => e.HireDate.Day)
                    .ToListAsync();

                var last7 = (await attRepo.GetLast7DaysAllAsync()).ToList();
                var labels = new string[7];
                var presentCounts = new int[7];
                for (var i = 0; i < 7; i++)
                {
                    var day = today.AddDays(-(6 - i));
                    labels[i] = day.ToString("ddd dd/MM");
                    presentCounts[i] = last7.Count(a => a.Date.Date == day.Date && AttendanceStatuses.IsPresentOrLate(a.Status));
                }

                var salaries = await db.Salaries
                    .Include(s => s.Employee)
                    .Where(s => s.Month == today.Month && s.Year == today.Year)
                    .ToListAsync();

                var departments = await db.Departments
                    .Include(d => d.Manager)
                    .ToListAsync();

                var departmentHeadcounts = departments
                    .Select(dept => new
                    {
                        DeptName = dept.DeptName,
                        Count = db.Employees.Count(e => e.DepartmentID == dept.DepartmentID && e.Status == EmployeeStatus.Active)
                    })
                    .Where(x => x.Count > 0)
                    .ToList();

                var departmentBudgets = departments
                    .Select(dept => new
                    {
                        DeptName = dept.DeptName,
                        TotalBudget = salaries
                            .Where(s => s.Employee != null && s.Employee.DepartmentID == dept.DepartmentID)
                            .Sum(s => s.NetSalary)
                    })
                    .Where(x => x.TotalBudget > 0)
                    .ToList();

                var approved = await db.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Approved);
                var pending = await db.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Pending);
                var rejected = await db.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Rejected);

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    RecentActivities.Clear();
                    foreach (var log in logs)
                    {
                        RecentActivities.Add(log);
                    }

                    UpcomingBirthdays.Clear();
                    foreach (var emp in birthdays)
                    {
                        UpcomingBirthdays.Add(emp);
                    }

                    UpcomingAnniversaries.Clear();
                    foreach (var emp in anniversaries)
                    {
                        UpcomingAnniversaries.Add(emp);
                    }

                    AttendanceTrendLabels = labels;
                    AttendanceTrendSeries.Clear();
                    AttendanceTrendSeries.Add(new LineSeries
                    {
                        Title = "Số người đi làm",
                        Values = new ChartValues<int>(presentCounts),
                        PointGeometrySize = 10,
                        StrokeThickness = 3
                    });

                    DepartmentDistributionSeries.Clear();
                    foreach (var dept in departmentHeadcounts)
                    {
                        DepartmentDistributionSeries.Add(new PieSeries
                        {
                            Title = dept.DeptName ?? "Khác",
                            Values = new ChartValues<int> { dept.Count }
                        });
                    }

                    if (!DepartmentDistributionSeries.Any())
                    {
                        DepartmentDistributionSeries.Add(new PieSeries
                        {
                            Title = "Chưa có dữ liệu",
                            Values = new ChartValues<int> { 1 }
                        });
                    }

                    BudgetDistributionSeries.Clear();
                    foreach (var dept in departmentBudgets)
                    {
                        BudgetDistributionSeries.Add(new ColumnSeries
                        {
                            Title = dept.DeptName,
                            Values = new ChartValues<decimal> { dept.TotalBudget },
                            DataLabels = true
                        });
                    }

                    LeaveStatusLabels = new[] { "Đã duyệt", "Chờ duyệt", "Từ chối" };
                   
                    LeaveStatusSeries.Clear();
                    LeaveStatusSeries.Add(new ColumnSeries { Title = "Đã duyệt", Values = new ChartValues<int> { approved } });
                    LeaveStatusSeries.Add(new ColumnSeries { Title = "Chờ duyệt", Values = new ChartValues<int> { pending } });
                    LeaveStatusSeries.Add(new ColumnSeries { Title = "Từ chối", Values = new ChartValues<int> { rejected } });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminDashboard LoadStatistics error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void OnTotalEmployeesCardClick() => NavigateToEmployees?.Invoke();
        public void OnTotalDepartmentsCardClick() => NavigateToDepartments?.Invoke();
        public void OnPendingLeaveCardClick() => NavigateToLeaves?.Invoke();
        public void OnAttendanceCardClick() => NavigateToAttendance?.Invoke();
    }
}

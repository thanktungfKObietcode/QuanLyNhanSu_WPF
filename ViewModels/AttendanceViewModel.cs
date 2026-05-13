using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Repositories;
using QuanLyNhanSu_WPF.Services;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class AttendanceViewModel : BaseViewModel
    {
        private bool _isAdmin;
        private bool _isLoading;
        private DateTime _fromDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime _toDate = DateTime.Today;
        private ObservableCollection<Attendance> _records = new();
        private ObservableCollection<Employee> _employees = new();
        private Employee _selectedEmployee;
        private Attendance _selectedRecord;
        private DateTime _markDate = DateTime.Today;
        private string _markCheckIn = "08:00";
        private string _markCheckOut = "17:00";
        private string _markStatus = AttendanceStatuses.Present;
        private double _markOvertime;

        public bool IsAdmin { get => _isAdmin; set => SetProperty(ref _isAdmin, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
        public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }
        public ObservableCollection<Attendance> Records { get => _records; set => SetProperty(ref _records, value); }
        public ObservableCollection<Employee> Employees { get => _employees; set => SetProperty(ref _employees, value); }
        public Employee SelectedEmployee { get => _selectedEmployee; set => SetProperty(ref _selectedEmployee, value); }
        public Attendance SelectedRecord { get => _selectedRecord; set => SetProperty(ref _selectedRecord, value); }
        public DateTime MarkDate { get => _markDate; set => SetProperty(ref _markDate, value); }
        public string MarkCheckIn { get => _markCheckIn; set => SetProperty(ref _markCheckIn, value); }
        public string MarkCheckOut { get => _markCheckOut; set => SetProperty(ref _markCheckOut, value); }
        public string MarkStatus { get => _markStatus; set => SetProperty(ref _markStatus, value); }
        public double MarkOvertime { get => _markOvertime; set => SetProperty(ref _markOvertime, value); }

        public ICommand LoadCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand MarkAttendanceCommand { get; }

        public ObservableCollection<string> StatusOptions { get; } = new(AttendanceStatuses.AllDisplayValues);

        private readonly AttendanceService _service;
        private readonly EmployeeRepository _empRepo;

        public AttendanceViewModel()
        {
            var db = new ApplicationDbContext(DbContextFactory.CreateOptions());
            _service = new AttendanceService(db);
            _empRepo = new EmployeeRepository(db);

            IsAdmin = SessionManager.Instance.CurrentUser?.Role == UserRole.Admin;

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            FilterCommand = new RelayCommand(async _ => await LoadAsync());
            MarkAttendanceCommand = new RelayCommand(async _ => await MarkAttendanceAsync(), _ => IsAdmin && SelectedEmployee != null);

            Task.Run(InitAsync);
        }

        private async Task InitAsync()
        {
            if (IsAdmin)
            {
                var emps = await _empRepo.GetActiveAsync();
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Employees.Clear();
                    foreach (var e in emps)
                    {
                        Employees.Add(e);
                    }
                });
            }

            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var empId = IsAdmin ? SelectedEmployee?.EmployeeID : null;
                var data = await _service.GetAttendanceAsync(empId, FromDate, ToDate);
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Records.Clear();
                    foreach (var r in data)
                    {
                        Records.Add(r);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task MarkAttendanceAsync()
        {
            if (SelectedEmployee == null)
                return;

            try
            {
                TimeSpan? checkIn = TimeSpan.TryParse(MarkCheckIn, out var ci) ? ci : null;
                TimeSpan? checkOut = TimeSpan.TryParse(MarkCheckOut, out var co) ? co : null;

                await _service.MarkAttendanceAsync(
                    SelectedEmployee.EmployeeID, MarkDate, checkIn, checkOut, MarkStatus, MarkOvertime);

                await LoadAsync();
                MessageBox.Show("Đã chấm công thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
            }
        }
    }
}

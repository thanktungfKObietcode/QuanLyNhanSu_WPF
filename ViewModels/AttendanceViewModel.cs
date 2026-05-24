using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        private string _markNote;

        public bool IsAdmin { get => _isAdmin; set => SetProperty(ref _isAdmin, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
        public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }
        public ObservableCollection<Attendance> Records { get => _records; set => SetProperty(ref _records, value); }
        public ObservableCollection<Employee> Employees { get => _employees; set => SetProperty(ref _employees, value); }
        public Employee SelectedEmployee { get => _selectedEmployee; set => SetProperty(ref _selectedEmployee, value); }

        public Attendance SelectedRecord
        {
            get => _selectedRecord;
            set
            {
                if (SetProperty(ref _selectedRecord, value) && value != null)
                {
                    LoadFormFromRecord(value);
                }
            }
        }

        public DateTime MarkDate
        {
            get => _markDate;
            set
            {
                if (SetProperty(ref _markDate, value))
                {
                    RefreshCalculatedFields();
                }
            }
        }

        public string MarkCheckIn
        {
            get => _markCheckIn;
            set
            {
                if (SetProperty(ref _markCheckIn, value))
                {
                    RefreshCalculatedFields();
                }
            }
        }

        public string MarkCheckOut
        {
            get => _markCheckOut;
            set
            {
                if (SetProperty(ref _markCheckOut, value))
                {
                    RefreshCalculatedFields();
                }
            }
        }

        public string MarkStatus
        {
            get => _markStatus;
            set
            {
                if (SetProperty(ref _markStatus, value))
                {
                    RefreshCalculatedFields();
                }
            }
        }

        public double MarkOvertime { get => _markOvertime; set => SetProperty(ref _markOvertime, value); }
        public string MarkNote { get => _markNote; set => SetProperty(ref _markNote, value); }

        public string WorkingHoursPreview
        {
            get
            {
                var checkIn = ParseTime(MarkCheckIn);
                var checkOut = ParseTime(MarkCheckOut);
                return Helpers.AttendanceCalculations.CalculateWorkingHours(checkIn, checkOut).ToString("0.##");
            }
        }

        public string WorkUnitsPreview
        {
            get
            {
                var checkIn = ParseTime(MarkCheckIn);
                var checkOut = ParseTime(MarkCheckOut);
                return Helpers.AttendanceCalculations.CalculateDailyWorkUnits(MarkStatus, checkIn, checkOut).ToString("0.##");
            }
        }

        public bool IsEditing => SelectedRecord != null;

        public string FormModeText => IsEditing ? "Dang chinh sua" : "Them moi";

        public ICommand LoadCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand AddAttendanceCommand { get; }
        public ICommand UpdateAttendanceCommand { get; }
        public ICommand DeleteAttendanceCommand { get; }
        public ICommand ResetFormCommand { get; }

        public ObservableCollection<string> StatusOptions { get; } = new(AttendanceStatuses.AllDisplayValues);

        public AttendanceViewModel()
        {
            IsAdmin = SessionManager.Instance.CurrentUser?.Role == UserRole.Admin;

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            FilterCommand = new RelayCommand(async _ => await LoadAsync());
            AddAttendanceCommand = new RelayCommand(async _ => await AddAttendanceAsync(), _ => IsAdmin && SelectedEmployee != null);
            UpdateAttendanceCommand = new RelayCommand(async _ => await UpdateAttendanceAsync(), _ => IsAdmin && SelectedRecord != null && SelectedEmployee != null);
            DeleteAttendanceCommand = new RelayCommand(async _ => await DeleteAttendanceAsync(), _ => IsAdmin && SelectedRecord != null);
            ResetFormCommand = new RelayCommand(_ => ResetForm(), _ => IsAdmin);

            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            if (IsAdmin)
            {
                var emps = await CreateEmployeeRepository().GetActiveAsync();
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Employees.Clear();
                    foreach (var e in emps)
                        Employees.Add(e);
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
                var data = await CreateAttendanceService().GetAttendanceAsync(empId, FromDate, ToDate);
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Records.Clear();
                    foreach (var r in data)
                        Records.Add(r);
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

        private async Task AddAttendanceAsync()
        {
            if (SelectedEmployee == null)
                return;

            try
            {
                var result = await CreateAttendanceService().CreateAttendanceAsync(
                    SelectedEmployee.EmployeeID,
                    MarkDate,
                    ParseTime(MarkCheckIn),
                    ParseTime(MarkCheckOut),
                    MarkStatus,
                    MarkOvertime,
                    MarkNote);

                if (!result.Success)
                {
                    MessageBox.Show(result.Error, "Khong the them cham cong", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await LoadAsync();
                ResetForm();
                MessageBox.Show("Da them ban ghi cham cong thanh cong.", "Thong bao", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi: {ex.Message}");
            }
        }

        private async Task UpdateAttendanceAsync()
        {
            if (SelectedRecord == null || SelectedEmployee == null)
                return;

            try
            {
                var result = await CreateAttendanceService().UpdateAttendanceAsync(
                    SelectedRecord.AttendanceID,
                    SelectedEmployee.EmployeeID,
                    MarkDate,
                    ParseTime(MarkCheckIn),
                    ParseTime(MarkCheckOut),
                    MarkStatus,
                    MarkOvertime,
                    MarkNote);

                if (!result.Success)
                {
                    MessageBox.Show(result.Error, "Khong the cap nhat cham cong", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await LoadAsync();
                ResetForm();
                MessageBox.Show("Da cap nhat ban ghi cham cong thanh cong.", "Thong bao", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi: {ex.Message}");
            }
        }

        private async Task DeleteAttendanceAsync()
        {
            if (SelectedRecord == null)
                return;

            var confirm = MessageBox.Show(
                $"Xoa ban ghi cham cong ngay {SelectedRecord.Date:dd/MM/yyyy} cua {SelectedRecord.Employee?.Name}?",
                "Xac nhan xoa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                await CreateAttendanceService().DeleteAttendanceAsync(SelectedRecord.AttendanceID);
                await LoadAsync();
                ResetForm();
                MessageBox.Show("Da xoa ban ghi cham cong.", "Thong bao", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi: {ex.Message}");
            }
        }

        private void LoadFormFromRecord(Attendance record)
        {
            if (record == null)
                return;

            if (record.Employee != null)
            {
                SelectedEmployee = Employees.FirstOrDefault(e => e.EmployeeID == record.EmployeeID) ?? record.Employee;
            }

            MarkDate = record.Date;
            MarkCheckIn = record.CheckIn?.ToString(@"hh\:mm") ?? string.Empty;
            MarkCheckOut = record.CheckOut?.ToString(@"hh\:mm") ?? string.Empty;
            MarkStatus = record.Status;
            MarkOvertime = record.OvertimeHours;
            MarkNote = record.Note;
            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(FormModeText));
        }

        private void ResetForm()
        {
            SelectedRecord = null;
            MarkDate = DateTime.Today;
            MarkCheckIn = "08:00";
            MarkCheckOut = "17:00";
            MarkStatus = AttendanceStatuses.Present;
            MarkOvertime = 0;
            MarkNote = null;
            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(FormModeText));
        }

        private static TimeSpan? ParseTime(string input)
            => TimeSpan.TryParse(input, out var value) ? value : null;

        private void RefreshCalculatedFields()
        {
            OnPropertyChanged(nameof(WorkingHoursPreview));
            OnPropertyChanged(nameof(WorkUnitsPreview));
        }

        private static AttendanceService CreateAttendanceService()
            => new(new ApplicationDbContext(DbContextFactory.CreateOptions()));

        private static EmployeeRepository CreateEmployeeRepository()
            => new(new ApplicationDbContext(DbContextFactory.CreateOptions()));
    }
}

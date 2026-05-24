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
    public class SalaryViewModel : BaseViewModel
    {
        private bool _isAdmin;
        private bool _isLoading;
        private int _selectedMonth = DateTime.Today.Month;
        private int _selectedYear = DateTime.Today.Year;
        private Salary _selectedSalary;
        private ObservableCollection<Salary> _salaries = new();
        private Salary _myLatestSalary;
        private ObservableCollection<DailySalaryEntry> _dailySalaryEntries = new();
        private ObservableCollection<MonthlySalarySummary> _monthlySalarySummaries = new();
        private DailySalaryEntry _selectedDailySalaryEntry;
        private MonthlySalarySummary _selectedMonthlySalarySummary;
        private ObservableCollection<Employee> _employees = new();
        private Employee _selectedEmployeeFilter;

        public bool IsAdmin { get => _isAdmin; set => SetProperty(ref _isAdmin, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public int SelectedMonth { get => _selectedMonth; set => SetProperty(ref _selectedMonth, value); }
        public int SelectedYear { get => _selectedYear; set => SetProperty(ref _selectedYear, value); }
        public Salary SelectedSalary { get => _selectedSalary; set => SetProperty(ref _selectedSalary, value); }
        public ObservableCollection<Salary> Salaries { get => _salaries; set => SetProperty(ref _salaries, value); }
        public Salary MyLatestSalary { get => _myLatestSalary; set => SetProperty(ref _myLatestSalary, value); }
        public ObservableCollection<Salary> MySalaryHistory { get; } = new();
        public ObservableCollection<DailySalaryEntry> DailySalaryEntries { get => _dailySalaryEntries; set => SetProperty(ref _dailySalaryEntries, value); }
        public ObservableCollection<MonthlySalarySummary> MonthlySalarySummaries { get => _monthlySalarySummaries; set => SetProperty(ref _monthlySalarySummaries, value); }
        public DailySalaryEntry SelectedDailySalaryEntry { get => _selectedDailySalaryEntry; set => SetProperty(ref _selectedDailySalaryEntry, value); }
        public MonthlySalarySummary SelectedMonthlySalarySummary { get => _selectedMonthlySalarySummary; set => SetProperty(ref _selectedMonthlySalarySummary, value); }
        public ObservableCollection<Employee> Employees { get => _employees; set => SetProperty(ref _employees, value); }
        public Employee SelectedEmployeeFilter { get => _selectedEmployeeFilter; set => SetProperty(ref _selectedEmployeeFilter, value); }

        public ICommand LoadCommand { get; }
        public ICommand CalculateSalaryCommand { get; }
        public ICommand ViewHistoryCommand { get; }
        public ICommand ExportMonthlyCommand { get; }
        public ICommand ExportDailyCommand { get; }

        public ObservableCollection<int> Months { get; } = new() { 1,2,3,4,5,6,7,8,9,10,11,12 };
        public ObservableCollection<int> Years { get; }

        private readonly ExcelExportService _exportService = new();

        public SalaryViewModel()
        {
            IsAdmin = SessionManager.Instance.CurrentUser?.Role == UserRole.Admin;

            Years = new ObservableCollection<int>();
            for (int y = DateTime.Today.Year; y >= DateTime.Today.Year - 5; y--)
                Years.Add(y);

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            CalculateSalaryCommand = new RelayCommand(async _ => await CalculateAllAsync(), _ => IsAdmin);
            ViewHistoryCommand = new RelayCommand(async _ => await LoadMyHistoryAsync());
            ExportMonthlyCommand = new RelayCommand(_ => ExportMonthlySummaries());
            ExportDailyCommand = new RelayCommand(_ => ExportDailySalaries());

            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            if (IsAdmin)
            {
                var employees = await CreateEmployeeRepository().GetActiveAsync();
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Employees.Clear();
                    foreach (var employee in employees)
                        Employees.Add(employee);
                });
            }

            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                if (IsAdmin)
                {
                    var employeeId = SelectedEmployeeFilter?.EmployeeID;
                    var salaryService = CreateSalaryService();
                    var dailyRows = await salaryService.GetDailySalaryEntriesAsync(SelectedMonth, SelectedYear, employeeId);
                    var monthlyRows = await salaryService.GetMonthlySalarySummariesAsync(SelectedMonth, SelectedYear, employeeId);
                    var officialRows = await salaryService.GetMonthlyPayrollAsync(SelectedMonth, SelectedYear);

                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        DailySalaryEntries.Clear();
                        foreach (var row in dailyRows)
                            DailySalaryEntries.Add(row);

                        MonthlySalarySummaries.Clear();
                        foreach (var row in monthlyRows)
                            MonthlySalarySummaries.Add(row);

                        Salaries.Clear();
                        foreach (var salary in officialRows)
                        {
                            if (!employeeId.HasValue || salary.EmployeeID == employeeId.Value)
                                Salaries.Add(salary);
                        }
                    });
                }
                else
                {
                    MyLatestSalary = await CreateSalaryService().GetMyLatestSalaryAsync();
                    await LoadMyHistoryAsync();

                    var salaryService = CreateSalaryService();
                    var dailyRows = await salaryService.GetDailySalaryEntriesAsync(SelectedMonth, SelectedYear);
                    var monthlyRows = await salaryService.GetMonthlySalarySummariesAsync(SelectedMonth, SelectedYear);

                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        DailySalaryEntries.Clear();
                        foreach (var row in dailyRows)
                            DailySalaryEntries.Add(row);

                        MonthlySalarySummaries.Clear();
                        foreach (var row in monthlyRows)
                            MonthlySalarySummaries.Add(row);
                    });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CalculateAllAsync()
        {
            var result = MessageBox.Show(
                $"Tinh luong thang {SelectedMonth}/{SelectedYear} cho tat ca nhan vien?",
                "Xac nhan",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            IsLoading = true;
            try
            {
                var employees = await CreateEmployeeRepository().GetActiveAsync();
                var count = 0;
                foreach (var employee in employees)
                {
                    await CreateSalaryService().CalculateMonthlySalaryAsync(employee, SelectedMonth, SelectedYear);
                    count++;
                }

                await LoadAsync();
                MessageBox.Show($"Da tinh luong cho {count} nhan vien.", "Hoan thanh", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadMyHistoryAsync()
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null || !user.EmployeeID.HasValue)
                return;

            try
            {
                var history = await CreateSalaryService().GetSalaryHistoryAsync(user.EmployeeID.Value);
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MySalaryHistory.Clear();
                    foreach (var salary in history)
                        MySalaryHistory.Add(salary);
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExportDailySalaries()
        {
            _exportService.ExportToExcel(
                DailySalaryEntries,
                "LuongNgay",
                $"LuongNgay_Thang{SelectedMonth}_{SelectedYear}.xlsx",
                (ws, data) =>
                {
                    ws.Cell(1, 1).Value = "Ngay";
                    ws.Cell(1, 2).Value = "Nhan vien";
                    ws.Cell(1, 3).Value = "Ma NV";
                    ws.Cell(1, 4).Value = "Phong ban";
                    ws.Cell(1, 5).Value = "Trang thai";
                    ws.Cell(1, 6).Value = "Cong ngay";
                    ws.Cell(1, 7).Value = "Gio lam";
                    ws.Cell(1, 8).Value = "OT";
                    ws.Cell(1, 9).Value = "Luong ngay";
                    ws.Cell(1, 10).Value = "Luong OT";
                    ws.Cell(1, 11).Value = "Tong ngay";

                    var row = 2;
                    foreach (var item in data)
                    {
                        ws.Cell(row, 1).Value = item.Date.ToString("dd/MM/yyyy");
                        ws.Cell(row, 2).Value = item.EmployeeName;
                        ws.Cell(row, 3).Value = item.EmployeeCode;
                        ws.Cell(row, 4).Value = item.DepartmentName;
                        ws.Cell(row, 5).Value = item.AttendanceStatus;
                        ws.Cell(row, 6).Value = item.WorkUnits;
                        ws.Cell(row, 7).Value = item.WorkingHours;
                        ws.Cell(row, 8).Value = item.OvertimeHours;
                        ws.Cell(row, 9).Value = item.BaseAmount;
                        ws.Cell(row, 10).Value = item.OvertimeAmount;
                        ws.Cell(row, 11).Value = item.TotalAmount;
                        row++;
                    }
                });
        }

        private void ExportMonthlySummaries()
        {
            _exportService.ExportToExcel(
                MonthlySalarySummaries,
                "LuongThang",
                $"LuongThang_Thang{SelectedMonth}_{SelectedYear}.xlsx",
                (ws, data) =>
                {
                    ws.Cell(1, 1).Value = "Nhan vien";
                    ws.Cell(1, 2).Value = "Ma NV";
                    ws.Cell(1, 3).Value = "Phong ban";
                    ws.Cell(1, 4).Value = "Tong cong";
                    ws.Cell(1, 5).Value = "Tong gio lam";
                    ws.Cell(1, 6).Value = "Tong OT";
                    ws.Cell(1, 7).Value = "Luong theo ngay";
                    ws.Cell(1, 8).Value = "Luong OT";
                    ws.Cell(1, 9).Value = "Phu cap co dinh";
                    ws.Cell(1, 10).Value = "Tong tam tinh";
                    ws.Cell(1, 11).Value = "Luong chot";

                    var row = 2;
                    foreach (var item in data)
                    {
                        ws.Cell(row, 1).Value = item.EmployeeName;
                        ws.Cell(row, 2).Value = item.EmployeeCode;
                        ws.Cell(row, 3).Value = item.DepartmentName;
                        ws.Cell(row, 4).Value = item.TotalWorkUnits;
                        ws.Cell(row, 5).Value = item.TotalWorkingHours;
                        ws.Cell(row, 6).Value = item.TotalOvertimeHours;
                        ws.Cell(row, 7).Value = item.DailySalaryTotal;
                        ws.Cell(row, 8).Value = item.OvertimeSalaryTotal;
                        ws.Cell(row, 9).Value = item.FixedAllowances;
                        ws.Cell(row, 10).Value = item.ProjectedNetSalary;
                        ws.Cell(row, 11).Value = item.OfficialNetSalary;
                        row++;
                    }
                });
        }

        private static SalaryService CreateSalaryService()
            => new(new ApplicationDbContext(DbContextFactory.CreateOptions()));

        private static EmployeeRepository CreateEmployeeRepository()
            => new(new ApplicationDbContext(DbContextFactory.CreateOptions()));
    }
}

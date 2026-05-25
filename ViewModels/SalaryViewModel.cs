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
        public Salary SelectedSalary
        {
            get => _selectedSalary;
            set
            {
                if (SetProperty(ref _selectedSalary, value))
                {
                    OnPropertyChanged(nameof(SelectedSalaryPeriod));
                }
            }
        }
        public ObservableCollection<Salary> Salaries { get => _salaries; set => SetProperty(ref _salaries, value); }
        public Salary MyLatestSalary { get => _myLatestSalary; set => SetProperty(ref _myLatestSalary, value); }
        public ObservableCollection<Salary> MySalaryHistory { get; } = new();
        public ObservableCollection<DailySalaryEntry> DailySalaryEntries { get => _dailySalaryEntries; set => SetProperty(ref _dailySalaryEntries, value); }
        public ObservableCollection<MonthlySalarySummary> MonthlySalarySummaries { get => _monthlySalarySummaries; set => SetProperty(ref _monthlySalarySummaries, value); }
        public DailySalaryEntry SelectedDailySalaryEntry { get => _selectedDailySalaryEntry; set => SetProperty(ref _selectedDailySalaryEntry, value); }
        public MonthlySalarySummary SelectedMonthlySalarySummary
        {
            get => _selectedMonthlySalarySummary;
            set
            {
                if (SetProperty(ref _selectedMonthlySalarySummary, value))
                {
                    OnPropertyChanged(nameof(SelectedSalaryPeriod));
                }
            }
        }

        public string SelectedSalaryPeriod
            => SelectedSalary != null ? $"Tháng {SelectedSalary.Month}/{SelectedSalary.Year}" :
               SelectedMonthlySalarySummary != null ? $"Tháng {SelectedMonthlySalarySummary.Month}/{SelectedMonthlySalarySummary.Year}" : "--";
        public ObservableCollection<Employee> Employees { get => _employees; set => SetProperty(ref _employees, value); }
        public Employee SelectedEmployeeFilter { get => _selectedEmployeeFilter; set => SetProperty(ref _selectedEmployeeFilter, value); }

        public ICommand LoadCommand { get; }
        public ICommand CalculateSalaryCommand { get; }
        public ICommand ExportPayslipCommand { get; }
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
            ExportPayslipCommand = new RelayCommand(_ => ExportPayslip(), _ => SelectedSalary != null || SelectedMonthlySalarySummary != null);
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
                    ws.Cell(1, 5).Value = "Luong theo ngay";
                    ws.Cell(1, 6).Value = "Luong OT";
                    ws.Cell(1, 7).Value = "KPI";
                    ws.Cell(1, 8).Value = "Hoa hong";
                    ws.Cell(1, 9).Value = "Bonus";
                    ws.Cell(1, 10).Value = "Phu cap";
                    ws.Cell(1, 11).Value = "Khau tru";
                    ws.Cell(1, 12).Value = "Tam tinh";
                    ws.Cell(1, 13).Value = "Da chot";

                    var row = 2;
                    foreach (var item in data)
                    {
                        ws.Cell(row, 1).Value = item.EmployeeName;
                        ws.Cell(row, 2).Value = item.EmployeeCode;
                        ws.Cell(row, 3).Value = item.DepartmentName;
                        ws.Cell(row, 4).Value = item.TotalWorkUnits;
                        ws.Cell(row, 5).Value = item.DailySalaryTotal;
                        ws.Cell(row, 6).Value = item.OvertimeSalaryTotal;
                        ws.Cell(row, 7).Value = item.KPIBonus;
                        ws.Cell(row, 8).Value = item.Commission;
                        ws.Cell(row, 9).Value = item.Bonus;
                        ws.Cell(row, 10).Value = item.FixedAllowances;
                        ws.Cell(row, 11).Value = item.Deductions;
                        ws.Cell(row, 12).Value = item.ProjectedNetSalary;
                        ws.Cell(row, 13).Value = item.OfficialNetSalary;
                        row++;
                    }
                });
        }

        private void ExportPayslip()
        {
            // prefer SelectedSalary (official) if present, otherwise use selected monthly summary
            if (SelectedSalary == null && SelectedMonthlySalarySummary == null)
                return;

            var isOfficial = SelectedSalary != null;
            var empName = isOfficial ? SelectedSalary.Employee?.Name : SelectedMonthlySalarySummary.EmployeeName;
            var empCode = isOfficial ? SelectedSalary.Employee?.Code : SelectedMonthlySalarySummary.EmployeeCode;
            var month = isOfficial ? SelectedSalary.Month : SelectedMonthlySalarySummary.Month;
            var year = isOfficial ? SelectedSalary.Year : SelectedMonthlySalarySummary.Year;

            var filename = $"PhieuLuong_{empCode}_Thang{month}_{year}.xlsx";

            if (isOfficial)
            {
                var salary = SelectedSalary;
                _exportService.ExportToExcel(new[] { salary }, "PhieuLuong", filename, (ws, data) =>
                {
                    ws.Cell(1, 1).Value = "PHIẾU LƯƠNG";
                    ws.Cell(2, 1).Value = "Nhân viên:";
                    ws.Cell(2, 2).Value = salary.Employee?.Name;
                    ws.Cell(3, 1).Value = "Mã NV:";
                    ws.Cell(3, 2).Value = salary.Employee?.Code;
                    ws.Cell(4, 1).Value = "Kỳ lương:";
                    ws.Cell(4, 2).Value = $"Tháng {salary.Month}/{salary.Year}";

                    ws.Cell(6, 1).Value = "Lương cơ bản";
                    ws.Cell(6, 2).Value = salary.BaseSalary;
                    ws.Cell(7, 1).Value = "Phụ cấp";
                    ws.Cell(7, 2).Value = salary.Allowances;
                    ws.Cell(8, 1).Value = "KPI";
                    ws.Cell(8, 2).Value = salary.KPIBonus;
                    ws.Cell(9, 1).Value = "Hoa hồng";
                    ws.Cell(9, 2).Value = salary.Commission;
                    ws.Cell(10, 1).Value = "OT";
                    ws.Cell(10, 2).Value = salary.OTSalary;
                    ws.Cell(11, 1).Value = "Khấu trừ";
                    ws.Cell(11, 2).Value = salary.Deductions;
                    ws.Cell(13, 1).Value = "Thực lĩnh";
                    ws.Cell(13, 2).Value = salary.NetSalary;
                });
            }
            else
            {
                var s = SelectedMonthlySalarySummary;
                _exportService.ExportToExcel(new[] { s }, "PhieuLuongTamTinh", filename, (ws, data) =>
                {
                    ws.Cell(1, 1).Value = "PHIẾU LƯƠNG (TẠM TÍNH)";
                    ws.Cell(2, 1).Value = "Nhân viên:";
                    ws.Cell(2, 2).Value = s.EmployeeName;
                    ws.Cell(3, 1).Value = "Mã NV:";
                    ws.Cell(3, 2).Value = s.EmployeeCode;
                    ws.Cell(4, 1).Value = "Kỳ lương:";
                    ws.Cell(4, 2).Value = $"Tháng {s.Month}/{s.Year}";

                    ws.Cell(6, 1).Value = "Lương ngày";
                    ws.Cell(6, 2).Value = s.DailySalaryTotal;
                    ws.Cell(7, 1).Value = "Lương OT";
                    ws.Cell(7, 2).Value = s.OvertimeSalaryTotal;
                    ws.Cell(8, 1).Value = "Phụ cấp";
                    ws.Cell(8, 2).Value = s.FixedAllowances;
                    ws.Cell(9, 1).Value = "KPI";
                    ws.Cell(9, 2).Value = s.KPIBonus;
                    ws.Cell(10, 1).Value = "Hoa hồng";
                    ws.Cell(10, 2).Value = s.Commission;
                    ws.Cell(11, 1).Value = "Khấu trừ";
                    ws.Cell(11, 2).Value = s.Deductions;
                    ws.Cell(13, 1).Value = "Tạm tính";
                    ws.Cell(13, 2).Value = s.ProjectedNetSalary;
                    ws.Cell(14, 1).Value = "(Lưu ý: Đây là tạm tính)";
                });
            }
        }

        private static SalaryService CreateSalaryService()
            => new(new ApplicationDbContext(DbContextFactory.CreateOptions()));

        private static EmployeeRepository CreateEmployeeRepository()
            => new(new ApplicationDbContext(DbContextFactory.CreateOptions()));
    }
}

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
using ClosedXML.Excel;
using Microsoft.Win32;

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

        public bool IsAdmin { get => _isAdmin; set => SetProperty(ref _isAdmin, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public int SelectedMonth { get => _selectedMonth; set => SetProperty(ref _selectedMonth, value); }
        public int SelectedYear { get => _selectedYear; set => SetProperty(ref _selectedYear, value); }
        public Salary SelectedSalary { get => _selectedSalary; set => SetProperty(ref _selectedSalary, value); }
        public ObservableCollection<Salary> Salaries { get => _salaries; set => SetProperty(ref _salaries, value); }
        public Salary MyLatestSalary { get => _myLatestSalary; set => SetProperty(ref _myLatestSalary, value); }
        public ObservableCollection<Salary> MySalaryHistory { get; } = new();

        public ICommand LoadCommand { get; }
        public ICommand CalculateSalaryCommand { get; }
        public ICommand ViewHistoryCommand { get; }
        public ICommand ExportCommand { get; }

        public ObservableCollection<int> Months { get; } = new() { 1,2,3,4,5,6,7,8,9,10,11,12 };
        public ObservableCollection<int> Years { get; }

        private readonly SalaryService _salaryService;
        private readonly EmployeeRepository _empRepo;

        public SalaryViewModel()
        {
            var db = new ApplicationDbContext(DbContextFactory.CreateOptions());
            _salaryService = new SalaryService(db);
            _empRepo = new EmployeeRepository(db);
            IsAdmin = SessionManager.Instance.CurrentUser?.Role == UserRole.Admin;

            Years = new ObservableCollection<int>();
            for (int y = DateTime.Today.Year; y >= DateTime.Today.Year - 5; y--) Years.Add(y);

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            CalculateSalaryCommand = new RelayCommand(async _ => await CalculateAllAsync(), _ => IsAdmin);
            ViewHistoryCommand = new RelayCommand(async _ => await LoadMyHistoryAsync());
            ExportCommand = new RelayCommand(_ => ExportSalaries());

            Task.Run(LoadAsync);
        }

        private void ExportSalaries()
        {
            var exportService = new ExcelExportService();
            string fileName = $"BangLuong_Thang{SelectedMonth}_{SelectedYear}.xlsx";
            exportService.ExportToExcel(Salaries, "BangLuong", fileName, (ws, data) =>
            {
                ws.Cell(1, 1).Value = "Nhân viên";
                ws.Cell(1, 2).Value = "Mã NV";
                ws.Cell(1, 3).Value = "Tháng/Năm";
                ws.Cell(1, 4).Value = "Lương cơ bản";
                ws.Cell(1, 5).Value = "Phụ cấp";
                ws.Cell(1, 6).Value = "Thưởng";
                ws.Cell(1, 7).Value = "Khấu trừ";
                ws.Cell(1, 8).Value = "Thực lĩnh";

                int row = 2;
                foreach (var s in data)
                {
                    ws.Cell(row, 1).Value = s.Employee?.Name;
                    ws.Cell(row, 2).Value = s.Employee?.Code;
                    ws.Cell(row, 3).Value = $"{s.Month}/{s.Year}";
                    ws.Cell(row, 4).Value = s.BaseSalary;
                    ws.Cell(row, 5).Value = s.Allowances;
                    ws.Cell(row, 6).Value = s.Bonus;
                    ws.Cell(row, 7).Value = s.Deductions;
                    ws.Cell(row, 8).Value = s.NetSalary;
                    
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0 \"₫\"";
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0 \"₫\"";
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0 \"₫\"";
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0 \"₫\"";
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 \"₫\"";
                    row++;
                }
            });
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                if (IsAdmin)
                {
                    var list = await _salaryService.GetMonthlyPayrollAsync(SelectedMonth, SelectedYear);
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        Salaries.Clear(); foreach (var s in list) Salaries.Add(s);
                    });
                }
                else
                {
                    MyLatestSalary = await _salaryService.GetMyLatestSalaryAsync();
                    await LoadMyHistoryAsync();
                }
            }
            catch (UnauthorizedAccessException ex) { MessageBox.Show(ex.Message); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            finally { IsLoading = false; }
        }

        private async Task CalculateAllAsync()
        {
            var result = MessageBox.Show(
                $"Tính lương tháng {SelectedMonth}/{SelectedYear} cho tất cả nhân viên?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var employees = await _empRepo.GetActiveAsync();
                int count = 0;
                foreach (var emp in employees)
                {
                    await _salaryService.CalculateMonthlySalaryAsync(emp, SelectedMonth, SelectedYear);
                    count++;
                }
                await LoadAsync();
                MessageBox.Show($"Đã tính lương cho {count} nhân viên.", "Hoàn thành", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
            finally { IsLoading = false; }
        }

        private async Task LoadMyHistoryAsync()
        {
            var user = SessionManager.Instance.CurrentUser;
            if (!user.EmployeeID.HasValue) return;
            try
            {
                var history = await _salaryService.GetSalaryHistoryAsync(user.EmployeeID.Value);
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MySalaryHistory.Clear();
                    foreach (var s in history) MySalaryHistory.Add(s);
                });
            }
            catch (UnauthorizedAccessException ex) { MessageBox.Show(ex.Message); }
        }
    }
}

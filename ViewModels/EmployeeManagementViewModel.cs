using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Services;
using ClosedXML.Excel;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class EmployeeManagementViewModel : BaseViewModel
    {
        private ObservableCollection<Employee> _employees = new();
        private Employee _selectedEmployee;
        private string _searchText;
        private bool _isLoading;
        private bool _canAdd, _canEdit, _canDelete;

        public ObservableCollection<Employee> Employees { get => _employees; set => SetProperty(ref _employees, value); }
        public Employee SelectedEmployee { get => _selectedEmployee; set => SetProperty(ref _selectedEmployee, value); }
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); SearchCommand.Execute(null); } }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public bool CanAdd { get => _canAdd; set => SetProperty(ref _canAdd, value); }
        public bool CanEdit { get => _canEdit; set => SetProperty(ref _canEdit, value); }
        public bool CanDelete { get => _canDelete; set => SetProperty(ref _canDelete, value); }

        public ICommand LoadCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CreateUserAccountCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }

        public event Action<Employee> AddRequested;
        public event Action<Employee> EditRequested;
        public event Action<Employee> ViewDetailsRequested;
        public event Action<string> UserAccountCreated;
        public event Action<string> ErrorOccurred;

        private readonly EmployeeService _service;

        public EmployeeManagementViewModel()
        {
            var db = new ApplicationDbContext(DbContextFactory.CreateOptions());
            _service = new EmployeeService(db);

            // Check permissions
            CanAdd = HasPermission(Permissions.Add_Employee);
            CanEdit = HasPermission(Permissions.Edit_Employee);
            CanDelete = HasPermission(Permissions.Delete_Employee);

            LoadCommand = new RelayCommand(async _ => await LoadEmployeesAsync());
            SearchCommand = new RelayCommand(async _ => await SearchAsync());
            RefreshCommand = new RelayCommand(async _ => await LoadEmployeesAsync());
            ExportCommand = new RelayCommand(_ => ExportEmployees());

            AddCommand = new RelayCommand(_ => AddRequested?.Invoke(new Employee()), _ => CanAdd);
            EditCommand = new RelayCommand(
                _ => { if (SelectedEmployee != null) EditRequested?.Invoke(SelectedEmployee); },
                _ => CanEdit && SelectedEmployee != null);

            DeleteCommand = new RelayCommand(async _ => await DeactivateAsync(), _ => CanDelete && SelectedEmployee != null);
            CreateUserAccountCommand = new RelayCommand(async _ => await CreateUserAccountAsync(), _ => SelectedEmployee != null);
            ViewDetailsCommand = new RelayCommand(_ => { if (SelectedEmployee != null) ViewDetailsRequested?.Invoke(SelectedEmployee); });

            Task.Run(LoadEmployeesAsync);
        }

        private void ExportEmployees()
        {
            var exportService = new ExcelExportService();
            exportService.ExportToExcel(Employees, "NhanVien", "DanhSachNhanVien.xlsx", (ws, data) =>
            {
                ws.Cell(1, 1).Value = "Mã NV";
                ws.Cell(1, 2).Value = "Họ và Tên";
                ws.Cell(1, 3).Value = "Giới tính";
                ws.Cell(1, 4).Value = "Ngày sinh";
                ws.Cell(1, 5).Value = "Điện thoại";
                ws.Cell(1, 6).Value = "Email";
                ws.Cell(1, 7).Value = "Phòng ban";
                ws.Cell(1, 8).Value = "Chức vụ";
                ws.Cell(1, 9).Value = "Trạng thái";

                int row = 2;
                foreach (var emp in data)
                {
                    ws.Cell(row, 1).Value = emp.Code;
                    ws.Cell(row, 2).Value = emp.Name;
                    ws.Cell(row, 3).Value = emp.Gender;
                    ws.Cell(row, 4).Value = emp.DateOfBirth.ToString("dd/MM/yyyy");
                    ws.Cell(row, 5).Value = emp.PhoneNumber;
                    ws.Cell(row, 6).Value = emp.Email;
                    ws.Cell(row, 7).Value = emp.Department?.DeptName;
                    ws.Cell(row, 8).Value = emp.Position?.PosName;
                    ws.Cell(row, 9).Value = emp.Status.ToString();
                    row++;
                }
            });
        }

        private async Task LoadEmployeesAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _service.GetAllAsync();
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Employees.Clear();
                    foreach (var e in list) Employees.Add(e);
                });
            }
            catch (UnauthorizedAccessException)
            {
                ErrorOccurred?.Invoke("Bạn không có quyền xem danh sách nhân viên.");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Lỗi: {ex.Message}");
            }
            finally { IsLoading = false; }
        }

        private async Task SearchAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _service.SearchAsync(SearchText ?? "");
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Employees.Clear();
                    foreach (var e in list) Employees.Add(e);
                });
            }
            finally { IsLoading = false; }
        }

        private async Task DeactivateAsync()
        {
            if (SelectedEmployee == null) return;
            var emp = SelectedEmployee;
            var result = MessageBox.Show(
                $"Vô hiệu hóa nhân viên {emp.Name}?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await _service.DeactivateAsync(emp.EmployeeID);
                await LoadEmployeesAsync();
            }
            catch (Exception ex) { ErrorOccurred?.Invoke(ex.Message); }
        }

        private async Task CreateUserAccountAsync()
        {
            if (SelectedEmployee == null) return;
            try
            {
                var (success, error, pwd) = await _service.CreateUserAccountAsync(SelectedEmployee.EmployeeID);
                if (success)
                    UserAccountCreated?.Invoke($"Tạo tài khoản thành công!\nTên đăng nhập: {SelectedEmployee.Code.ToLower()}\nMật khẩu: {pwd}");
                else
                    ErrorOccurred?.Invoke(error);
            }
            catch (Exception ex) { ErrorOccurred?.Invoke(ex.Message); }
        }

        public async Task RefreshAfterSave()
        {
            await LoadEmployeesAsync();
        }
    }
}

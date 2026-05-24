using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Services;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class EmployeeManagementViewModel : BaseViewModel
    {
        private ObservableCollection<Employee> _employees = new();
        private Employee _selectedEmployee;
        private string _searchText;
        private bool _isLoading;
        private bool _canAdd;
        private bool _canEdit;
        private bool _canDelete;

        public ObservableCollection<Employee> Employees { get => _employees; set => SetProperty(ref _employees, value); }
        public Employee SelectedEmployee { get => _selectedEmployee; set => SetProperty(ref _selectedEmployee, value); }
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                SearchCommand.Execute(null);
            }
        }
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

        public EmployeeManagementViewModel()
        {
            CanAdd = HasPermission(Permissions.Add_Employee);
            CanEdit = HasPermission(Permissions.Edit_Employee);
            CanDelete = HasPermission(Permissions.Delete_Employee);

            LoadCommand = new RelayCommand(async _ => await LoadEmployeesAsync());
            SearchCommand = new RelayCommand(async _ => await SearchAsync());
            RefreshCommand = new RelayCommand(async _ => await LoadEmployeesAsync());
            ExportCommand = new RelayCommand(_ => ExportEmployees());

            AddCommand = new RelayCommand(_ => AddRequested?.Invoke(new Employee()), _ => CanAdd);
            EditCommand = new RelayCommand(
                _ =>
                {
                    if (SelectedEmployee != null)
                    {
                        EditRequested?.Invoke(SelectedEmployee);
                    }
                },
                _ => CanEdit && SelectedEmployee != null);

            DeleteCommand = new RelayCommand(async _ => await DeleteEmployeeAsync(), _ => CanDelete && SelectedEmployee != null);
            CreateUserAccountCommand = new RelayCommand(async param => await CreateUserAccountAsync(param as Employee));
            ViewDetailsCommand = new RelayCommand(_ =>
            {
                if (SelectedEmployee != null)
                {
                    ViewDetailsRequested?.Invoke(SelectedEmployee);
                }
            });

            _ = LoadEmployeesAsync();
        }

        private void ExportEmployees()
        {
            var exportService = new ExcelExportService();
            exportService.ExportToExcel(Employees, "NhanVien", "DanhSachNhanVien.xlsx", (ws, data) =>
            {
                ws.Cell(1, 1).Value = "Ma NV";
                ws.Cell(1, 2).Value = "Ho va Ten";
                ws.Cell(1, 3).Value = "Gioi tinh";
                ws.Cell(1, 4).Value = "Ngay sinh";
                ws.Cell(1, 5).Value = "Dien thoai";
                ws.Cell(1, 6).Value = "Email";
                ws.Cell(1, 7).Value = "Phong ban";
                ws.Cell(1, 8).Value = "Chuc vu";
                ws.Cell(1, 9).Value = "Tai khoan";
                ws.Cell(1, 10).Value = "Trang thai";

                var row = 2;
                foreach (var employee in data)
                {
                    ws.Cell(row, 1).Value = employee.Code;
                    ws.Cell(row, 2).Value = employee.Name;
                    ws.Cell(row, 3).Value = employee.Gender;
                    ws.Cell(row, 4).Value = employee.DateOfBirth.ToString("dd/MM/yyyy");
                    ws.Cell(row, 5).Value = employee.PhoneNumber;
                    ws.Cell(row, 6).Value = employee.Email;
                    ws.Cell(row, 7).Value = employee.Department?.DeptName;
                    ws.Cell(row, 8).Value = employee.Position?.PosName;
                    ws.Cell(row, 9).Value = employee.HasAccount ? "Da co" : "Chua co";
                    ws.Cell(row, 10).Value = employee.Status.ToString();
                    row++;
                }
            });
        }

        private async Task LoadEmployeesAsync()
        {
            IsLoading = true;
            try
            {
                var list = await CreateEmployeeService().GetAllAsync();
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Employees.Clear();
                    foreach (var employee in list)
                    {
                        Employees.Add(employee);
                    }
                });
            }
            catch (UnauthorizedAccessException)
            {
                ErrorOccurred?.Invoke("Ban khong co quyen xem danh sach nhan vien.");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Loi: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchAsync()
        {
            IsLoading = true;
            try
            {
                var list = await CreateEmployeeService().SearchAsync(SearchText ?? string.Empty);
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Employees.Clear();
                    foreach (var employee in list)
                    {
                        Employees.Add(employee);
                    }
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteEmployeeAsync()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            var employee = SelectedEmployee;
            var result = MessageBox.Show(
                $"Xoa hoan toan nhan vien {employee.Name}?\n\nToan bo du lieu cham cong, nghi phep, luong va tai khoan lien quan se bi xoa.",
                "Xac nhan xoa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                await CreateEmployeeService().DeactivateAsync(employee.EmployeeID);
                await LoadEmployeesAsync();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex.Message);
            }
        }

        private async Task CreateUserAccountAsync(Employee employee)
        {
            var target = employee ?? SelectedEmployee;
            if (target == null)
            {
                return;
            }

            var confirm = MessageBox.Show(
                $"Cap tai khoan dang nhap cho nhan vien {target.Name}?\nTen dang nhap se la: {target.Code.ToLower()}",
                "Xac nhan",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var (success, error, password) = await CreateEmployeeService().CreateUserAccountAsync(target.EmployeeID);
                if (success)
                {
                    await LoadEmployeesAsync();
                    UserAccountCreated?.Invoke(
                        $"Tao tai khoan thanh cong!\n\nTen dang nhap: {target.Code.ToLower()}\nMat khau mac dinh: {password}\n\nHay cung cap thong tin nay cho nhan vien.");
                }
                else
                {
                    ErrorOccurred?.Invoke(error);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task RefreshAfterSave()
        {
            await LoadEmployeesAsync();
        }

        private static EmployeeService CreateEmployeeService()
            => new(new ApplicationDbContext(DbContextFactory.CreateOptions()));
    }
}

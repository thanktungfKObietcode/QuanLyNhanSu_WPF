using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Services;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class EmployeeFormViewModel : BaseViewModel
    {
        private Employee _employee;
        private bool _isEditMode;
        private bool _isSaving;
        private string _errorMessage;
        private ObservableCollection<Department> _departments = new();
        private ObservableCollection<Position> _positions = new();

        public Employee Employee { get => _employee; set => SetProperty(ref _employee, value); }
        public bool IsEditMode { get => _isEditMode; set => SetProperty(ref _isEditMode, value); }
        public bool IsSaving { get => _isSaving; set => SetProperty(ref _isSaving, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public ObservableCollection<Department> Departments { get => _departments; set => SetProperty(ref _departments, value); }
        public ObservableCollection<Position> Positions { get => _positions; set => SetProperty(ref _positions, value); }
        public bool IsSalesPosition => Employee?.Position?.PosName?.ToLower().Contains("sale") == true || Employee?.Position?.PosName?.ToLower().Contains("kinh doanh") == true;

        public ObservableCollection<string> GenderOptions { get; } = new() { "Nam", "Nữ", "Khác" };
        public ObservableCollection<EmployeeStatus> StatusOptions { get; } = new()
        { EmployeeStatus.Active, EmployeeStatus.Inactive };
        public ObservableCollection<EmploymentType> EmploymentTypeOptions { get; } = new()
        { EmploymentType.Permanent, EmploymentType.Probation, EmploymentType.PartTime };

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowsePhotoCommand { get; }

        public event Action<bool> Closed;

        private readonly EmployeeService _service;
        private readonly ApplicationDbContext _db;

        public EmployeeFormViewModel(Employee employee = null)
        {
            _db = new ApplicationDbContext(DbContextFactory.CreateOptions());
            _service = new EmployeeService(_db);

            IsEditMode = employee != null && employee.EmployeeID > 0;
            Employee = employee ?? new Employee { HireDate = DateTime.Today, Status = EmployeeStatus.Active };

            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsSaving);
            CancelCommand = new RelayCommand(_ => Closed?.Invoke(false));
            BrowsePhotoCommand = new RelayCommand(_ => BrowsePhoto());

            Task.Run(LoadDropdownsAsync);
        }

        private async Task LoadDropdownsAsync()
        {
            var depts = await _db.Departments.ToListAsync();
            var positions = await _db.Positions.ToListAsync();
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Departments.Clear(); foreach (var d in depts) Departments.Add(d);
                Positions.Clear(); foreach (var p in positions) Positions.Add(p);
            });
        }

        private async Task SaveAsync()
        {
            ErrorMessage = null;
            IsSaving = true;
            try
            {
                (bool success, string error) result;
                if (IsEditMode)
                    result = await _service.UpdateEmployeeAsync(Employee);
                else
                    result = await _service.AddEmployeeAsync(Employee);

                if (result.success)
                    Closed?.Invoke(true);
                else
                    ErrorMessage = result.error;
            }
            catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
            finally { IsSaving = false; }
        }

        private void BrowsePhoto()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Chọn ảnh nhân viên"
            };
            if (dlg.ShowDialog() == true)
            {
                var savedPath = _service.SavePhoto(dlg.FileName, Employee.EmployeeID);
                if (savedPath != null) Employee.Photo = savedPath;
            }
        }
    }
}

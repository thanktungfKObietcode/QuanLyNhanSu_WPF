using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private DateTime? _dateOfBirth;
        private DateTime? _hireDate;
        private ObservableCollection<Department> _departments = new();
        private ObservableCollection<Position> _positions = new();

        public Employee Employee
        {
            get => _employee;
            set
            {
                if (SetProperty(ref _employee, value))
                {
                    OnPropertyChanged(nameof(IsSalesPosition));
                    // Sync DateTime properties from Employee
                    if (value != null)
                    {
                        _dateOfBirth = value.DateOfBirth;
                        _hireDate = value.HireDate;
                        OnPropertyChanged(nameof(DateOfBirth));
                        OnPropertyChanged(nameof(HireDate));
                    }
                }
            }
        }

        public DateTime? DateOfBirth
        {
            get => _dateOfBirth;
            set
            {
                if (SetProperty(ref _dateOfBirth, value))
                {
                    if (_employee != null && value.HasValue)
                    {
                        _employee.DateOfBirth = value.Value;
                    }
                }
            }
        }

        public DateTime? HireDate
        {
            get => _hireDate;
            set
            {
                if (SetProperty(ref _hireDate, value))
                {
                    if (_employee != null && value.HasValue)
                    {
                        _employee.HireDate = value.Value;
                    }
                }
            }
        }

        public bool IsEditMode { get => _isEditMode; set => SetProperty(ref _isEditMode, value); }
        public bool IsSaving { get => _isSaving; set => SetProperty(ref _isSaving, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public ObservableCollection<Department> Departments { get => _departments; set => SetProperty(ref _departments, value); }
        public ObservableCollection<Position> Positions { get => _positions; set => SetProperty(ref _positions, value); }

        public bool IsSalesPosition
        {
            get
            {
                var positionName = Positions
                    .FirstOrDefault(p => p.PositionID == Employee?.PositionID)?
                    .PosName?
                    .ToLowerInvariant();

                return positionName?.Contains("sale") == true || positionName?.Contains("kinh doanh") == true;
            }
        }

        public ObservableCollection<string> GenderOptions { get; } = new() { "Nam", "Nu", "Khac" };
        public ObservableCollection<EmployeeStatus> StatusOptions { get; } = new()
        { EmployeeStatus.Active, EmployeeStatus.Inactive };
        public ObservableCollection<EmploymentType> EmploymentTypeOptions { get; } = new()
        { EmploymentType.Permanent, EmploymentType.Probation, EmploymentType.PartTime };

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowsePhotoCommand { get; }

        public event Action<bool> Closed;

        public EmployeeFormViewModel(Employee employee = null)
        {
            IsEditMode = employee != null && employee.EmployeeID > 0;
            Employee = employee ?? new Employee { 
                HireDate = DateTime.Today, 
                Status = EmployeeStatus.Active,
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsSaving);
            CancelCommand = new RelayCommand(_ => Closed?.Invoke(false));
            BrowsePhotoCommand = new RelayCommand(_ => BrowsePhoto());

            _ = LoadDropdownsAsync();
        }

        private async Task LoadDropdownsAsync()
        {
            await using var db = new ApplicationDbContext(DbContextFactory.CreateOptions());
            var depts = await db.Departments.AsNoTracking().ToListAsync();
            var positions = await db.Positions.AsNoTracking().ToListAsync();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Departments.Clear();
                foreach (var d in depts)
                {
                    Departments.Add(d);
                }

                Positions.Clear();
                foreach (var p in positions)
                {
                    Positions.Add(p);
                }

                OnPropertyChanged(nameof(IsSalesPosition));
            });
        }

        private async Task SaveAsync()
        {
            ErrorMessage = null;
            IsSaving = true;

            try
            {
                // Ensure Employee has the latest values from DatePicker properties
                if (_dateOfBirth.HasValue)
                {
                    Employee.DateOfBirth = _dateOfBirth.Value;
                }
                if (_hireDate.HasValue)
                {
                    Employee.HireDate = _hireDate.Value;
                }

                (bool success, string error) result;
                var service = CreateEmployeeService();
                if (IsEditMode)
                {
                    result = await service.UpdateEmployeeAsync(Employee);
                }
                else
                {
                    result = await service.AddEmployeeAsync(Employee);
                }

                if (result.success)
                {
                    var actionText = IsEditMode ? "cap nhat" : "them moi";
                    MessageBox.Show(
                        $"Da {actionText} thong tin nhan vien '{Employee.Name}' thanh cong.",
                        "Thanh cong",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Closed?.Invoke(true);
                }
                else
                {
                    ErrorMessage = result.error;
                    MessageBox.Show(
                        result.error,
                        "Khong the luu thong tin",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = BuildDetailedErrorMessage(ex);
                MessageBox.Show(
                    ErrorMessage,
                    "Chi tiet loi luu nhan vien",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }

        private static string BuildDetailedErrorMessage(Exception ex)
        {
            var lines = new List<string>();
            var current = ex;
            var level = 0;

            while (current != null)
            {
                var prefix = level == 0 ? "Loi" : $"Inner {level}";
                lines.Add($"{prefix}: {current.Message}");
                current = current.InnerException;
                level++;
            }

            if (ex is DbUpdateException dbUpdateException && dbUpdateException.Entries.Count > 0)
            {
                foreach (var entry in dbUpdateException.Entries)
                {
                    lines.Add($"Entity: {entry.Metadata.DisplayName()}");
                    lines.Add($"State: {entry.State}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void BrowsePhoto()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Chon anh nhan vien"
            };

            if (dlg.ShowDialog() == true)
            {
                var savedPath = CreateEmployeeService().SavePhoto(dlg.FileName, Employee.EmployeeID);
                if (savedPath != null)
                {
                    Employee.Photo = savedPath;
                }
            }
        }

        private static EmployeeService CreateEmployeeService()
            => new(new ApplicationDbContext(DbContextFactory.CreateOptions()));
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class DepartmentPositionViewModel : BaseViewModel
    {
        private ObservableCollection<Department> _departments = new();
        private ObservableCollection<Position> _positions = new();
        private Department _selectedDepartment;
        private Position _selectedPosition;
        private bool _isLoading;

        public ObservableCollection<Department> Departments { get => _departments; set => SetProperty(ref _departments, value); }
        public ObservableCollection<Position> Positions { get => _positions; set => SetProperty(ref _positions, value); }
        public Department SelectedDepartment { get => _selectedDepartment; set => SetProperty(ref _selectedDepartment, value); }
        public Position SelectedPosition { get => _selectedPosition; set => SetProperty(ref _selectedPosition, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        public ICommand LoadCommand { get; }
        public ICommand AddDepartmentCommand { get; }
        public ICommand DeleteDepartmentCommand { get; }
        public ICommand AddPositionCommand { get; }
        public ICommand DeletePositionCommand { get; }

        public DepartmentPositionViewModel()
        {
            LoadCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddDepartmentCommand = new RelayCommand(async _ => await AddDepartmentAsync());
            DeleteDepartmentCommand = new RelayCommand(async _ => await DeleteDepartmentAsync(), _ => SelectedDepartment != null);
            AddPositionCommand = new RelayCommand(async _ => await AddPositionAsync());
            DeletePositionCommand = new RelayCommand(async _ => await DeletePositionAsync(), _ => SelectedPosition != null);

            Task.Run(LoadDataAsync);
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var options = DbContextFactory.CreateOptions();
                await using var db = new ApplicationDbContext(options);
                var depts = await db.Departments.ToListAsync();
                var pos = await db.Positions.ToListAsync();

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Departments.Clear();
                    foreach (var d in depts) Departments.Add(d);
                    Positions.Clear();
                    foreach (var p in pos) Positions.Add(p);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
            finally { IsLoading = false; }
        }

        private async Task AddDepartmentAsync()
        {
            // Simple prompt could be better, but for simplicity we add a dummy and let them edit it inline if we use a DataGrid
            var newDept = new Department { DeptCode = "NEW", DeptName = "Phòng ban mới" };
            var options = DbContextFactory.CreateOptions();
            await using var db = new ApplicationDbContext(options);
            db.Departments.Add(newDept);
            await db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async Task DeleteDepartmentAsync()
        {
            if (SelectedDepartment == null) return;
            var options = DbContextFactory.CreateOptions();
            await using var db = new ApplicationDbContext(options);
            var dept = await db.Departments.FindAsync(SelectedDepartment.DepartmentID);
            if (dept != null)
            {
                var linkedEmployees = await db.Employees
                    .Where(e => e.DepartmentID == dept.DepartmentID)
                    .ToListAsync();

                if (linkedEmployees.Count > 0)
                {
                    var confirm = MessageBox.Show(
                        $"Phòng ban này đang được gán cho {linkedEmployees.Count} nhân viên.\n" +
                        "Nếu tiếp tục, phòng ban của các nhân viên đó sẽ được xóa khỏi hồ sơ.\n\n" +
                        "Bạn có muốn tiếp tục xóa phòng ban không?",
                        "Xác nhận xóa phòng ban",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (confirm != MessageBoxResult.Yes)
                        return;

                    foreach (var employee in linkedEmployees)
                    {
                        employee.DepartmentID = null;
                        employee.Department = null;
                    }
                }

                db.Departments.Remove(dept);
                await db.SaveChangesAsync();
                await LoadDataAsync();
            }
        }

        private async Task AddPositionAsync()
        {
            var newPos = new Position { PosCode = "NEW", PosName = "Chức vụ mới", BaseSalary = 0 };
            var options = DbContextFactory.CreateOptions();
            await using var db = new ApplicationDbContext(options);
            db.Positions.Add(newPos);
            await db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async Task DeletePositionAsync()
        {
            if (SelectedPosition == null) return;
            var options = DbContextFactory.CreateOptions();
            await using var db = new ApplicationDbContext(options);
            var pos = await db.Positions.FindAsync(SelectedPosition.PositionID);
            if (pos != null)
            {
                var linkedEmployees = await db.Employees
                    .Where(e => e.PositionID == pos.PositionID)
                    .ToListAsync();

                if (linkedEmployees.Count > 0)
                {
                    var confirm = MessageBox.Show(
                        $"Chức vụ này đang được gán cho {linkedEmployees.Count} nhân viên.\n" +
                        "Nếu tiếp tục, chức vụ của các nhân viên đó sẽ được xóa khỏi hồ sơ.\n\n" +
                        "Bạn có muốn tiếp tục xóa chức vụ không?",
                        "Xác nhận xóa chức vụ",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (confirm != MessageBoxResult.Yes)
                        return;

                    foreach (var employee in linkedEmployees)
                    {
                        employee.PositionID = null;
                        employee.Position = null;
                    }
                }

                db.Positions.Remove(pos);
                await db.SaveChangesAsync();
                await LoadDataAsync();
            }
        }

        // We can add SaveChangesCommand if using inline DataGrid editing
        public async Task SaveChangesAsync()
        {
            var options = DbContextFactory.CreateOptions();
            await using var db = new ApplicationDbContext(options);
            
            foreach (var d in Departments)
            {
                var entity = await db.Departments.FindAsync(d.DepartmentID);
                if (entity != null)
                {
                    entity.DeptCode = d.DeptCode;
                    entity.DeptName = d.DeptName;
                }
            }
            foreach (var p in Positions)
            {
                var entity = await db.Positions.FindAsync(p.PositionID);
                if (entity != null)
                {
                    entity.PosCode = p.PosCode;
                    entity.PosName = p.PosName;
                    entity.BaseSalary = p.BaseSalary;
                }
            }
            await db.SaveChangesAsync();
            MessageBox.Show("Đã lưu thay đổi!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

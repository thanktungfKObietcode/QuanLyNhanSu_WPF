using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Services;
using QuanLyNhanSu_WPF.Data;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class MenuItemViewModel
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Permission { get; set; }
        public string ViewKey { get; set; }
    }

    public class MainViewModel : BaseViewModel
    {
        private object _currentViewModel;
        private MenuItemViewModel _selectedMenuItem;
        private string _breadcrumb;
        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; } = new();
        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set { SetProperty(ref _currentViewModel, value); }
        }

        public MenuItemViewModel SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (SetProperty(ref _selectedMenuItem, value) && value != null)
                {
                    NavigateTo(value);
                }
            }
        }

        public string Breadcrumb
        {
            get => _breadcrumb;
            set => SetProperty(ref _breadcrumb, value);
        }

        public string DisplayName => CurrentUserInfo?.DisplayName;
        public string DisplayRole => CurrentUserInfo?.Role;
        public string Photo => CurrentUserInfo?.Photo;
        public UserInfoViewModel CurrentUserInfo { get; }

        private readonly ViewModelFactory _factory = new();

        public MainViewModel()
        {
            CurrentUserInfo = new UserInfoViewModel(SessionManager.Instance.CurrentUser);
            LoadMenuByRole();
            NavigateCommand = new RelayCommand(param => NavigateTo(param as MenuItemViewModel));
            LogoutCommand = new RelayCommand(_ => Logout());
            
            // Navigate to default dashboard
            var defaultKey = SessionManager.Instance.CurrentUser?.Role == UserRole.Admin ? "AdminDashboard" : "EmployeeDashboard";
            NavigateToKey(defaultKey);

            if (SessionManager.Instance.CurrentUser?.Role == UserRole.Admin)
            {
                _ = CheckBirthdayEmailsAsync();
            }
        }

        private async Task CheckBirthdayEmailsAsync()
        {
            try
            {
                var db = new ApplicationDbContext(DbContextFactory.CreateOptions());
                var emailService = new EmailService(db);
                await emailService.SendBirthdayEmailsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Birthday email error: {ex.Message}");
            }
        }

        public event Action LogoutRequested;

        public void LoadMenuByRole()
        {
            MenuItems.Clear();
            var role = SessionManager.Instance.CurrentUser?.Role;
            if (role == UserRole.Admin)
            {
                MenuItems.Add(new MenuItemViewModel { Title = "📊 Dashboard", Icon = "ViewDashboard", ViewKey = "AdminDashboard" });
                MenuItems.Add(new MenuItemViewModel { Title = "👥 Quản lý nhân viên", Icon = "AccountGroup", ViewKey = "EmployeeManagement" });
                MenuItems.Add(new MenuItemViewModel { Title = "🏢 Phòng ban & Chức vụ", Icon = "OfficeBuilding", ViewKey = "DepartmentPosition" });
                MenuItems.Add(new MenuItemViewModel { Title = "📅 Chấm công", Icon = "CalendarCheck", ViewKey = "Attendance" });
                MenuItems.Add(new MenuItemViewModel { Title = "📝 Duyệt nghỉ phép", Icon = "ClipboardCheck", ViewKey = "LeaveApproval" });
                MenuItems.Add(new MenuItemViewModel { Title = "💰 Quản lý lương", Icon = "CashMultiple", ViewKey = "SalaryManagement" });
                MenuItems.Add(new MenuItemViewModel { Title = "📧 Trung tâm Email", Icon = "EmailOutline", ViewKey = "EmailCenter" });
            }
            else if (role == UserRole.Employee)
            {
                MenuItems.Add(new MenuItemViewModel { Title = "📊 Trang chủ", Icon = "Home", ViewKey = "EmployeeDashboard" });
                MenuItems.Add(new MenuItemViewModel { Title = "📅 Chấm công của tôi", Icon = "CalendarCheck", ViewKey = "MyAttendance" });
                MenuItems.Add(new MenuItemViewModel { Title = "📝 Đăng ký nghỉ phép", Icon = "ClipboardText", ViewKey = "LeaveRequest" });
                MenuItems.Add(new MenuItemViewModel { Title = "💰 Bảng lương của tôi", Icon = "Cash", ViewKey = "MySalary" });
            }
        }

        private void NavigateTo(MenuItemViewModel menu)
        {
            if (menu == null) return;
            NavigateToKey(menu.ViewKey);
        }

        public void NavigateToKey(string viewKey)
        {
            try
            {
                CurrentViewModel = _factory.CreateViewModel(viewKey);
                var menu = MenuItems.FirstOrDefault(m => m.ViewKey == viewKey);
                Breadcrumb = menu?.Title ?? viewKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        private void Logout()
        {
            SessionManager.Instance.EndSession();
            LogoutRequested?.Invoke();
        }
    }

    public class UserInfoViewModel
    {
        public string DisplayName { get; }
        public string Role { get; }
        public string Photo { get; }
        public UserInfoViewModel(User user)
        {
            DisplayName = user?.Employee?.Name ?? user?.Username;
            Role = user?.Role.ToString();
            Photo = user?.Employee?.Photo;
        }
    }
}

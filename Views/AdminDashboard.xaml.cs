using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyNhanSu_WPF.ViewModels;

namespace QuanLyNhanSu_WPF.Views
{
    public partial class AdminDashboard : UserControl
    {
        private AdminDashboardViewModel _vm;

        public AdminDashboard()
        {
            InitializeComponent();
            _vm = new AdminDashboardViewModel();
            _vm.NavigateToEmployees   += () => NavigateTo("EmployeeManagement");
            _vm.NavigateToDepartments += () => NavigateTo("DepartmentPosition");
            _vm.NavigateToLeaves      += () => NavigateTo("LeaveApproval");
            _vm.NavigateToAttendance  += () => NavigateTo("Attendance");
            DataContext = _vm;
        }

        private void NavigateTo(string viewKey)
        {
            var mainVm = Application.Current?.MainWindow?.DataContext as MainViewModel;
            mainVm?.NavigateToKey(viewKey);
        }

        private void TotalEmployeesCard_Click(object sender, MouseButtonEventArgs e) => _vm.OnTotalEmployeesCardClick();
        private void TotalDepartmentsCard_Click(object sender, MouseButtonEventArgs e) => _vm.OnTotalDepartmentsCardClick();
        private void PendingLeaveCard_Click(object sender, MouseButtonEventArgs e) => _vm.OnPendingLeaveCardClick();
        private void AttendanceCard_Click(object sender, MouseButtonEventArgs e) => _vm.OnAttendanceCardClick();
        private void BtnManageEmployees_Click(object sender, RoutedEventArgs e) => NavigateTo("EmployeeManagement");
        private void BtnManageDepts_Click(object sender, RoutedEventArgs e) => NavigateTo("DepartmentPosition");
        private void BtnLeaveApproval_Click(object sender, RoutedEventArgs e) => NavigateTo("LeaveApproval");
        private void BtnAttendance_Click(object sender, RoutedEventArgs e) => NavigateTo("Attendance");
        private void BtnSalary_Click(object sender, RoutedEventArgs e) => NavigateTo("SalaryManagement");
    }
}

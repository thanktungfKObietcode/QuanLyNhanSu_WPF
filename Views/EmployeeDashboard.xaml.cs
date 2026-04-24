using System.Windows;
using System.Windows.Controls;
using QuanLyNhanSu_WPF.ViewModels;

namespace QuanLyNhanSu_WPF.Views
{
    public partial class EmployeeDashboard : UserControl
    {
        private EmployeeDashboardViewModel _vm;

        public EmployeeDashboard()
        {
            InitializeComponent();
            _vm = new EmployeeDashboardViewModel();
            _vm.RequestLeaveRequested += () => NavigateTo("LeaveRequest");
            _vm.ViewAttendanceRequested += () => NavigateTo("MyAttendance");
            DataContext = _vm;
        }

        private void NavigateTo(string viewKey)
        {
            var mainVm = Application.Current?.MainWindow?.DataContext as MainViewModel;
            mainVm?.NavigateToKey(viewKey);
        }

        private void BtnRequestLeave_Click(object sender, RoutedEventArgs e) => _vm.OnRequestLeave();
        private void BtnViewAttendance_Click(object sender, RoutedEventArgs e) => _vm.OnViewAttendance();
    }
}

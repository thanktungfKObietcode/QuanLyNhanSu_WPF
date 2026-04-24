using System.Windows;
using System.Windows.Controls;
using QuanLyNhanSu_WPF.ViewModels;

namespace QuanLyNhanSu_WPF.Views
{
    public partial class EmployeeManagementView : UserControl
    {
        private EmployeeManagementViewModel _vm;

        public EmployeeManagementView()
        {
            InitializeComponent();
            _vm = new EmployeeManagementViewModel();
            
            // Xử lý sự kiện khi tạo tài khoản thành công
            _vm.UserAccountCreated += (msg) => MessageBox.Show(msg, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Xử lý sự kiện lỗi
            _vm.ErrorOccurred += (msg) => MessageBox.Show(msg, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

            // Xử lý yêu cầu thêm/sửa nhân viên (Mở Window mới)
            _vm.AddRequested += (emp) => OpenEmployeeForm(emp);
            _vm.EditRequested += (emp) => OpenEmployeeForm(emp);

            DataContext = _vm;
        }

        private void OpenEmployeeForm(Models.Employee employee)
        {
            // Window này sẽ được tạo ở bước tiếp theo
            var formViewModel = new EmployeeFormViewModel(employee);
            var formWindow = new Window
            {
                Title = employee.EmployeeID == 0 ? "Thêm nhân viên mới" : "Chỉnh sửa nhân viên",
                Content = new EmployeeFormView { DataContext = formViewModel },
                Width = 600,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            formViewModel.Closed += async (success) =>
            {
                formWindow.Close();
                if (success) await _vm.RefreshAfterSave();
            };

            formWindow.ShowDialog();
        }
    }
}

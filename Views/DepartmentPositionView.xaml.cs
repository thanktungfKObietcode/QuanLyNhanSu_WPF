using System.Windows;
using System.Windows.Controls;
using QuanLyNhanSu_WPF.ViewModels;

namespace QuanLyNhanSu_WPF.Views
{
    public partial class DepartmentPositionView : UserControl
    {
        public DepartmentPositionView()
        {
            InitializeComponent();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DepartmentPositionViewModel vm)
            {
                await vm.SaveChangesAsync();
            }
        }
    }
}

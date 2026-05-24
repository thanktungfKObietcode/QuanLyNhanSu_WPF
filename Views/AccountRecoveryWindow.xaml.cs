using System.Windows;
using QuanLyNhanSu_WPF.ViewModels;

namespace QuanLyNhanSu_WPF.Views
{
    public partial class AccountRecoveryWindow : Window
    {
        public AccountRecoveryWindow()
        {
            InitializeComponent();

            var viewModel = new AccountRecoveryViewModel();
            viewModel.CloseRequested += Close;
            DataContext = viewModel;
        }
    }
}

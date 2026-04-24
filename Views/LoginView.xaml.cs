using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyNhanSu_WPF.ViewModels;

namespace QuanLyNhanSu_WPF.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Handle Enter key for PasswordBox
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Enter && PwdBox.IsFocused)
            {
                if (DataContext is LoginViewModel vm)
                {
                    vm.LoginCommand.Execute(null);
                }
            }
        }

        // Allow dragging the window
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }
    }
}

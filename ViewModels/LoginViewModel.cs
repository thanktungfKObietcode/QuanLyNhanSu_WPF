using System;
using System.Threading.Tasks;
using System.Windows.Input;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Services;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Data;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _username;
        private string _password;
        private bool _rememberMe;
        private string _errorMessage;
        private bool _isBusy;
        private readonly AuthenticationService _authService;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand ForgotPasswordCommand { get; }

        public event Action LoginSucceeded;
        public event Action ForgotPasswordRequested;

        public LoginViewModel()
        {
            var options = DbContextFactory.CreateOptions();
            var db = new ApplicationDbContext(options);
            _authService = new AuthenticationService(db);
            
            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => CanLogin());
            ForgotPasswordCommand = new RelayCommand(_ => ForgotPasswordRequested?.Invoke());
        }

        private bool CanLogin()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private async Task LoginAsync()
        {
            ErrorMessage = null;
            IsBusy = true;
            try
            {
                var result = await _authService.LoginAsync(Username.Trim(), Password);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }
                // RememberMe logic can be implemented here (e.g., save username to settings)
                LoginSucceeded?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

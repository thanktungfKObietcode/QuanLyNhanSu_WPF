using System;
using System.Threading.Tasks;
using System.Windows.Input;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Services;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class AccountRecoveryViewModel : BaseViewModel
    {
        private string _employeeCode;
        private string _employeeContact;
        private string _adminRecoveryKey;
        private string _resultMessage;
        private string _errorMessage;
        private bool _isBusy;

        public string EmployeeCode
        {
            get => _employeeCode;
            set => SetProperty(ref _employeeCode, value);
        }

        public string EmployeeContact
        {
            get => _employeeContact;
            set => SetProperty(ref _employeeContact, value);
        }

        public string AdminRecoveryKey
        {
            get => _adminRecoveryKey;
            set => SetProperty(ref _adminRecoveryKey, value);
        }

        public string ResultMessage
        {
            get => _resultMessage;
            set => SetProperty(ref _resultMessage, value);
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

        public ICommand RecoverEmployeeCommand { get; }
        public ICommand RecoverAdminCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action CloseRequested;

        public AccountRecoveryViewModel()
        {
            RecoverEmployeeCommand = new RelayCommand(async _ => await RecoverEmployeeAsync(), _ => !IsBusy);
            RecoverAdminCommand = new RelayCommand(async _ => await RecoverAdminAsync(), _ => !IsBusy);
            CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke());
        }

        private async Task RecoverEmployeeAsync()
        {
            await RunRecoveryAsync(service => service.RecoverEmployeeAccessAsync(EmployeeCode, EmployeeContact));
        }

        private async Task RecoverAdminAsync()
        {
            await RunRecoveryAsync(service => service.RecoverAdminAccessAsync(AdminRecoveryKey));
        }

        private async Task RunRecoveryAsync(Func<AuthenticationService, Task<AccountRecoveryResult>> action)
        {
            ResultMessage = null;
            ErrorMessage = null;
            IsBusy = true;

            try
            {
                var result = await action(CreateAuthenticationService());
                if (result.Succeeded)
                {
                    ResultMessage = result.Message;
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Khong the khoi phuc tai khoan: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static AuthenticationService CreateAuthenticationService()
            => new(new ApplicationDbContext(DbContextFactory.CreateOptions()));
    }
}

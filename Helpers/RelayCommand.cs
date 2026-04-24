using System;
using System.Windows.Input;
using QuanLyNhanSu_WPF.Services;

namespace QuanLyNhanSu_WPF.Helpers
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        private readonly string _requiredPermission;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null, string requiredPermission = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _requiredPermission = requiredPermission;
        }

        public bool CanExecute(object parameter)
        {
            if (!string.IsNullOrEmpty(_requiredPermission) && !AuthorizationService.Current.HasPermission(_requiredPermission))
                return false;
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}

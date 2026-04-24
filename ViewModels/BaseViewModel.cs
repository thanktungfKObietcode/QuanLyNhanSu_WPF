using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuanLyNhanSu_WPF.ViewModels
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    public class PermissionAttribute : Attribute
    {
        public string RequiredPermission { get; }
        public PermissionAttribute(string permission)
        {
            RequiredPermission = permission;
        }
    }

    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Authorization helpers
        public bool HasPermission(string permission)
        {
            return Services.AuthorizationService.Current.HasPermission(permission);
        }

        public void CheckPermission(string permission)
        {
            Services.AuthorizationService.Current.CheckPermission(permission);
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Services;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class EmployeeEmailSelection : BaseViewModel
    {
        private bool _isSelected;
        public Employee Employee { get; set; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public class EmailCenterViewModel : BaseViewModel
    {
        private string _subject;
        private string _body;
        private bool _isSending;
        private string _statusMessage;
        private readonly EmailService _emailService;

        public ObservableCollection<EmployeeEmailSelection> Employees { get; } = new();

        public string Subject
        {
            get => _subject;
            set => SetProperty(ref _subject, value);
        }

        public string Body
        {
            get => _body;
            set => SetProperty(ref _body, value);
        }

        public bool IsSending
        {
            get => _isSending;
            set => SetProperty(ref _isSending, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand SendCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }

        public EmailCenterViewModel()
        {
            var db = new ApplicationDbContext(DbContextFactory.CreateOptions());
            _emailService = new EmailService(db);

            SendCommand = new RelayCommand(async _ => await SendEmailsAsync(), CanSend);
            SelectAllCommand = new RelayCommand(_ => { foreach (var e in Employees) e.IsSelected = true; });
            DeselectAllCommand = new RelayCommand(_ => { foreach (var e in Employees) e.IsSelected = false; });

            _ = LoadEmployeesAsync(db);
        }

        private async Task LoadEmployeesAsync(ApplicationDbContext db)
        {
            var emps = await db.Employees
                .Where(e => e.Status == EmployeeStatus.Active && !string.IsNullOrEmpty(e.Email))
                .OrderBy(e => e.Name)
                .ToListAsync();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Employees.Clear();
                foreach (var emp in emps)
                {
                    Employees.Add(new EmployeeEmailSelection { Employee = emp, IsSelected = false });
                }
            });
        }

        private bool CanSend(object obj)
        {
            return !IsSending && 
                   !string.IsNullOrWhiteSpace(Subject) && 
                   !string.IsNullOrWhiteSpace(Body) && 
                   Employees.Any(e => e.IsSelected);
        }

        private async Task SendEmailsAsync()
        {
            var selectedEmployees = Employees.Where(e => e.IsSelected).ToList();
            if (!selectedEmployees.Any()) return;

            IsSending = true;
            StatusMessage = "Đang gửi email...";
            int successCount = 0;
            int failCount = 0;
            string lastError = string.Empty;

            foreach (var item in selectedEmployees)
            {
                var result = await _emailService.SendEmailAsync(item.Employee.Email, Subject, Body, item.Employee.EmployeeID);
                if (result.success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    lastError = result.error;
                }
            }

            IsSending = false;
            StatusMessage = $"Hoàn tất: {successCount} thành công, {failCount} thất bại.";
            
            if (failCount > 0)
            {
                var errorMsg = $"Có {failCount} email gửi thất bại.\n\nLỗi chi tiết: {lastError}\n\nLƯU Ý: Nếu dùng Gmail, bạn KHÔNG THỂ dùng mật khẩu đăng nhập bình thường. Bạn PHẢI tạo 'Mật khẩu ứng dụng' (App Password) gồm 16 ký tự trong cài đặt Bảo mật của tài khoản Google.";
                MessageBox.Show(errorMsg, "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show($"Đã gửi thành công {successCount} email!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                Subject = string.Empty;
                Body = string.Empty;
                foreach (var e in Employees) e.IsSelected = false;
            }
        }
    }
}

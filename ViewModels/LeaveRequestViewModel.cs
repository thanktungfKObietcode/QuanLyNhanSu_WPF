using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Services;

namespace QuanLyNhanSu_WPF.ViewModels
{
    public class LeaveRequestViewModel : BaseViewModel
    {
        private bool _isAdmin;
        private bool _isLoading;
        private LeaveRequest _selectedRequest;
        private int _selectedTabIndex;

        // New request form
        private string _newLeaveType = "Annual";
        private DateTime _newStartDate = DateTime.Today;
        private DateTime _newEndDate = DateTime.Today;
        private string _newReason;
        private string _approvalNotes;

        public bool IsAdmin { get => _isAdmin; set => SetProperty(ref _isAdmin, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public LeaveRequest SelectedRequest { get => _selectedRequest; set => SetProperty(ref _selectedRequest, value); }
        public int SelectedTabIndex { get => _selectedTabIndex; set => SetProperty(ref _selectedTabIndex, value); }
        public string NewLeaveType { get => _newLeaveType; set => SetProperty(ref _newLeaveType, value); }
        public DateTime NewStartDate { get => _newStartDate; set => SetProperty(ref _newStartDate, value); }
        public DateTime NewEndDate { get => _newEndDate; set => SetProperty(ref _newEndDate, value); }
        public string NewReason { get => _newReason; set => SetProperty(ref _newReason, value); }
        public string ApprovalNotes { get => _approvalNotes; set => SetProperty(ref _approvalNotes, value); }

        public ObservableCollection<LeaveRequest> AllRequests { get; } = new();
        public ObservableCollection<LeaveRequest> PendingRequests { get; } = new();
        public ObservableCollection<LeaveRequest> MyRequests { get; } = new();

        public ObservableCollection<string> LeaveTypes { get; } = new()
        { "Annual", "Sick", "Maternity", "Unpaid" };

        public ICommand LoadCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand SubmitRequestCommand { get; }
        public ICommand RefreshCommand { get; }

        private readonly LeaveRequestService _service;

        public LeaveRequestViewModel()
        {
            var db = new ApplicationDbContext(DbContextFactory.CreateOptions());
            _service = new LeaveRequestService(db);
            IsAdmin = SessionManager.Instance.CurrentUser?.Role == UserRole.Admin;

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            RefreshCommand = new RelayCommand(async _ => await LoadAsync());

            ApproveCommand = new RelayCommand(async _ => await ApproveAsync(),
                _ => IsAdmin && SelectedRequest?.Status == LeaveStatus.Pending);
            RejectCommand = new RelayCommand(async _ => await RejectAsync(),
                _ => IsAdmin && SelectedRequest?.Status == LeaveStatus.Pending);
            SubmitRequestCommand = new RelayCommand(async _ => await SubmitRequestAsync(), _ => !IsAdmin);

            Task.Run(LoadAsync);
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                if (IsAdmin)
                {
                    var all = await _service.GetAllAsync();
                    var pending = await _service.GetPendingAsync();
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        AllRequests.Clear(); foreach (var r in all) AllRequests.Add(r);
                        PendingRequests.Clear(); foreach (var r in pending) PendingRequests.Add(r);
                    });
                }
                else
                {
                    var my = await _service.GetMyRequestsAsync();
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        MyRequests.Clear(); foreach (var r in my) MyRequests.Add(r);
                    });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            finally { IsLoading = false; }
        }

        private async Task ApproveAsync()
        {
            if (SelectedRequest == null) return;
            var user = SessionManager.Instance.CurrentUser;
            var result = await _service.ApproveLeaveAsync(SelectedRequest.LeaveRequestID, user.UserID, ApprovalNotes ?? "");
            if (result) { await LoadAsync(); MessageBox.Show("Đã duyệt đơn nghỉ phép.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information); }
        }

        private async Task RejectAsync()
        {
            if (SelectedRequest == null) return;
            if (string.IsNullOrWhiteSpace(ApprovalNotes))
            {
                MessageBox.Show("Vui lòng nhập lý do từ chối.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var user = SessionManager.Instance.CurrentUser;
            var result = await _service.RejectLeaveAsync(SelectedRequest.LeaveRequestID, user.UserID, ApprovalNotes);
            if (result) { await LoadAsync(); MessageBox.Show("Đã từ chối đơn nghỉ phép.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information); }
        }

        private async Task SubmitRequestAsync()
        {
            var user = SessionManager.Instance.CurrentUser;
            if (!user.EmployeeID.HasValue) { MessageBox.Show("Tài khoản của bạn chưa được liên kết với nhân viên."); return; }

            if (string.IsNullOrWhiteSpace(NewReason))
            {
                MessageBox.Show("Vui lòng nhập lý do nghỉ phép.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (success, error) = await _service.CreateLeaveRequestAsync(
                user.EmployeeID.Value, NewLeaveType, NewStartDate, NewEndDate, NewReason.Trim());

            if (success)
            {
                NewReason = "";
                await LoadAsync();
                MessageBox.Show("Đã gửi đơn nghỉ phép thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
                MessageBox.Show(error, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Repositories;

namespace QuanLyNhanSu_WPF.Services
{
    public class LeaveRequestService
    {
        private readonly LeaveRequestRepository _repo;
        private const int AnnualLeaveDays = 12;

        public LeaveRequestService(ApplicationDbContext db)
        {
            _repo = new LeaveRequestRepository(db);
        }

        public async Task<(bool Success, string Error)> CreateLeaveRequestAsync(
            int employeeId, string leaveType, DateTime startDate, DateTime endDate, string reason)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Request_Leave);

            var user = SessionManager.Instance.CurrentUser;
            if (user.Role == UserRole.Employee && user.EmployeeID != employeeId)
                return (false, "Bạn chỉ có thể tạo đơn nghỉ phép cho chính mình.");

            if (employeeId <= 0)
                return (false, "Không xác định được nhân viên.");

            startDate = startDate.Date;
            endDate = endDate.Date;
            reason = reason?.Trim();

            if (string.IsNullOrWhiteSpace(reason))
                return (false, "Lý do nghỉ phép không được để trống.");

            if (endDate < startDate)
                return (false, "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");

            if (leaveType == LeaveTypes.Annual && startDate.Year != endDate.Year)
                return (false, "Đơn nghỉ phép năm phải nằm trong cùng một năm để tính hạn mức chính xác.");

            var hasOverlap = await _repo.HasOverlappingLeaveAsync(employeeId, startDate, endDate);
            if (hasOverlap)
                return (false, "Đã có đơn nghỉ phép trùng ngày. Vui lòng kiểm tra lại.");

            if (leaveType == LeaveTypes.Annual)
            {
                var usedDays = await _repo.GetApprovedLeaveDaysInYearAsync(employeeId, startDate.Year);
                var requestedDays = GetRequestedDays(startDate, endDate);
                if (usedDays + requestedDays > AnnualLeaveDays)
                    return (false, $"Không đủ ngày phép. Đã dùng {usedDays}/{AnnualLeaveDays} ngày.");
            }

            var request = new LeaveRequest
            {
                EmployeeID = employeeId,
                LeaveType = leaveType,
                StartDate = startDate,
                EndDate = endDate,
                Reason = reason,
                Status = LeaveStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            await _repo.AddAsync(request);
            await AuditLogger.LogCreateAsync("LeaveRequests", request.LeaveRequestID.ToString(), $"{leaveType} {startDate:dd/MM}-{endDate:dd/MM}");
            return (true, null);
        }

        public async Task<bool> ApproveLeaveAsync(int leaveId, int approvedByUserId, string notes)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Approve_Leave);

            var request = await _repo.GetByIdAsync(leaveId);
            if (request == null || request.Status != LeaveStatus.Pending)
                return false;

            request.Status = LeaveStatus.Approved;
            request.ApprovedByUserID = approvedByUserId;
            request.ApprovedDate = DateTime.UtcNow;
            request.ApprovalNote = notes?.Trim();
            await _repo.UpdateAsync(request);
            await AuditLogger.LogUpdateAsync("LeaveRequests", leaveId.ToString(), "Pending", "Approved");
            return true;
        }

        public async Task<bool> RejectLeaveAsync(int leaveId, int rejectedByUserId, string notes)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Approve_Leave);

            var request = await _repo.GetByIdAsync(leaveId);
            if (request == null || request.Status != LeaveStatus.Pending)
                return false;

            request.Status = LeaveStatus.Rejected;
            request.ApprovedByUserID = rejectedByUserId;
            request.ApprovedDate = DateTime.UtcNow;
            request.ApprovalNote = notes?.Trim();
            await _repo.UpdateAsync(request);
            await AuditLogger.LogUpdateAsync("LeaveRequests", leaveId.ToString(), "Pending", "Rejected");
            return true;
        }

        public async Task<int> CalculateRemainingLeavesAsync(int employeeId)
        {
            var usedDays = await _repo.GetApprovedLeaveDaysInYearAsync(employeeId, DateTime.Today.Year);
            return Math.Max(0, AnnualLeaveDays - usedDays);
        }

        public int CalculateRequestedDays(DateTime startDate, DateTime endDate)
            => GetRequestedDays(startDate.Date, endDate.Date);

        public async Task<IEnumerable<LeaveRequest>> GetAllAsync()
        {
            AuthorizationService.Current.CheckPermission(Permissions.View_All_Leaves);
            return await _repo.GetAllAsync();
        }

        public async Task<IEnumerable<LeaveRequest>> GetPendingAsync()
        {
            AuthorizationService.Current.CheckPermission(Permissions.Approve_Leave);
            return await _repo.GetPendingAsync();
        }

        public async Task<IEnumerable<LeaveRequest>> GetMyRequestsAsync()
        {
            var user = SessionManager.Instance.CurrentUser;
            if (!user.EmployeeID.HasValue)
                return new List<LeaveRequest>();

            return await _repo.GetByEmployeeAsync(user.EmployeeID.Value);
        }

        public async Task<int> GetPendingCountAsync()
            => await _repo.GetPendingCountAsync();

        private static int GetRequestedDays(DateTime startDate, DateTime endDate)
            => Math.Max(1, (endDate - startDate).Days + 1);
    }
}

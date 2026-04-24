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
        private const int AnnualLeaveDays = 12; // Standard annual leave days per year

        public LeaveRequestService(ApplicationDbContext db)
        {
            _repo = new LeaveRequestRepository(db);
        }

        /// <summary>Employee creates a leave request for themselves.</summary>
        public async Task<(bool Success, string Error)> CreateLeaveRequestAsync(
            int employeeId, string leaveType, DateTime startDate, DateTime endDate, string reason)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Request_Leave);

            var user = SessionManager.Instance.CurrentUser;
            // Employee can only create for themselves
            if (user.Role == UserRole.Employee && user.EmployeeID != employeeId)
                return (false, "Bạn chỉ có thể tạo đơn nghỉ phép cho chính mình.");

            if (endDate < startDate)
                return (false, "Ngày kết thúc phải sau ngày bắt đầu.");

            // Overlap check
            bool hasOverlap = await _repo.HasOverlappingLeaveAsync(employeeId, startDate, endDate);
            if (hasOverlap)
                return (false, "Đã có đơn nghỉ phép trùng ngày. Vui lòng kiểm tra lại.");

            // Check remaining days for annual leave
            if (leaveType == "Annual")
            {
                int usedDays = await _repo.GetApprovedLeaveDaysInYearAsync(employeeId, startDate.Year);
                int requestedDays = (endDate - startDate).Days + 1;
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

        /// <summary>Admin approves a leave request.</summary>
        public async Task<bool> ApproveLeaveAsync(int leaveId, int approvedByUserId, string notes)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Approve_Leave);

            var request = await _repo.GetByIdAsync(leaveId);
            if (request == null) return false;

            request.Status = LeaveStatus.Approved;
            request.ApprovedByUserID = approvedByUserId;
            request.ApprovedDate = DateTime.UtcNow;
            request.ApprovalNote = notes;
            await _repo.UpdateAsync(request);
            await AuditLogger.LogUpdateAsync("LeaveRequests", leaveId.ToString(), "Pending", "Approved");
            return true;
        }

        /// <summary>Admin rejects a leave request.</summary>
        public async Task<bool> RejectLeaveAsync(int leaveId, int rejectedByUserId, string notes)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Approve_Leave);

            var request = await _repo.GetByIdAsync(leaveId);
            if (request == null) return false;

            request.Status = LeaveStatus.Rejected;
            request.ApprovedByUserID = rejectedByUserId;
            request.ApprovedDate = DateTime.UtcNow;
            request.ApprovalNote = notes;
            await _repo.UpdateAsync(request);
            await AuditLogger.LogUpdateAsync("LeaveRequests", leaveId.ToString(), "Pending", "Rejected");
            return true;
        }

        /// <summary>Calculate remaining annual leave days for employee in current year.</summary>
        public async Task<int> CalculateRemainingLeavesAsync(int employeeId)
        {
            int usedDays = await _repo.GetApprovedLeaveDaysInYearAsync(employeeId, DateTime.Today.Year);
            return Math.Max(0, AnnualLeaveDays - usedDays);
        }

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
            if (!user.EmployeeID.HasValue) return new List<LeaveRequest>();
            return await _repo.GetByEmployeeAsync(user.EmployeeID.Value);
        }

        public async Task<int> GetPendingCountAsync()
            => await _repo.GetPendingCountAsync();
    }
}

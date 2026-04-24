using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Repositories
{
    public class LeaveRequestRepository
    {
        private readonly ApplicationDbContext _db;

        public LeaveRequestRepository(ApplicationDbContext db) => _db = db;

        public async Task<LeaveRequest> GetByIdAsync(int id)
            => await _db.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.ApprovedByUser)
                .FirstOrDefaultAsync(l => l.LeaveRequestID == id);

        public async Task<IEnumerable<LeaveRequest>> GetAllAsync()
            => await _db.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e.Department)
                .Include(l => l.ApprovedByUser)
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync();

        public async Task<IEnumerable<LeaveRequest>> GetPendingAsync()
            => await _db.LeaveRequests
                .Include(l => l.Employee)
                .Where(l => l.Status == LeaveStatus.Pending)
                .OrderBy(l => l.CreatedDate)
                .ToListAsync();

        public async Task<IEnumerable<LeaveRequest>> GetByEmployeeAsync(int employeeId)
            => await _db.LeaveRequests
                .Include(l => l.ApprovedByUser)
                .Where(l => l.EmployeeID == employeeId)
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync();

        public async Task<int> GetPendingCountAsync()
            => await _db.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Pending);

        public async Task<bool> HasOverlappingLeaveAsync(int employeeId, DateTime start, DateTime end, int? excludeId = null)
        {
            var query = _db.LeaveRequests
                .Where(l => l.EmployeeID == employeeId
                         && l.Status != LeaveStatus.Rejected
                         && l.Status != LeaveStatus.Cancelled
                         && l.StartDate <= end && l.EndDate >= start);
            if (excludeId.HasValue)
                query = query.Where(l => l.LeaveRequestID != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<int> GetApprovedLeaveDaysInYearAsync(int employeeId, int year)
        {
            var leaves = await _db.LeaveRequests
                .Where(l => l.EmployeeID == employeeId
                         && l.Status == LeaveStatus.Approved
                         && l.StartDate.Year == year
                         && l.LeaveType != "Maternity" && l.LeaveType != "Unpaid")
                .ToListAsync();
            return leaves.Sum(l => (l.EndDate - l.StartDate).Days + 1);
        }

        public async Task AddAsync(LeaveRequest request)
        {
            _db.LeaveRequests.Add(request);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(LeaveRequest request)
        {
            _db.LeaveRequests.Update(request);
            await _db.SaveChangesAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using QuanLyNhanSu_WPF.Repositories;

namespace QuanLyNhanSu_WPF.Services
{
    public class AttendanceService
    {
        private readonly AttendanceRepository _repo;
        private readonly int _currentEmployeeId;

        public AttendanceService(ApplicationDbContext db)
        {
            _repo = new AttendanceRepository(db);
            _currentEmployeeId = SessionManager.Instance.CurrentUser?.EmployeeID ?? 0;
        }

        /// <summary>
        /// Get attendance records. Admin sees all; Employee sees only own.
        /// </summary>
        public async Task<IEnumerable<Attendance>> GetAttendanceAsync(int? employeeId, DateTime from, DateTime to)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null) throw new UnauthorizedAccessException();

            if (user.Role == UserRole.Employee)
            {
                // Employee can only see their own data
                if (!user.EmployeeID.HasValue) return new List<Attendance>();
                return await _repo.GetByEmployeeAsync(user.EmployeeID.Value, from, to);
            }

            // Admin
            if (employeeId.HasValue)
                return await _repo.GetByEmployeeAsync(employeeId.Value, from, to);

            return await _repo.GetAllByDateRangeAsync(from, to);
        }

        /// <summary>Admin only: mark or update attendance for an employee.</summary>
        public async Task<bool> MarkAttendanceAsync(int employeeId, DateTime date, TimeSpan? checkIn, TimeSpan? checkOut, string status, string note = null)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Manage_Attendance);

            var record = new Attendance
            {
                EmployeeID = employeeId,
                Date = date.Date,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Status = status,
                Note = note
            };
            await _repo.UpsertAsync(record);
            await AuditLogger.LogCreateAsync("Attendances", $"{employeeId}/{date:yyyy-MM-dd}", status);
            return true;
        }

        public TimeSpan CalculateWorkingHours(TimeSpan? checkIn, TimeSpan? checkOut)
        {
            if (!checkIn.HasValue || !checkOut.HasValue) return TimeSpan.Zero;
            var diff = checkOut.Value - checkIn.Value;
            return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
        }

        public async Task<double> GetMonthlyAttendanceRateAsync(int month, int year)
            => await _repo.GetMonthlyAttendanceRateAsync(month, year);

        public async Task<int> GetPresentTodayCountAsync()
            => await _repo.GetPresentTodayCountAsync();

        public async Task<int> GetOnLeaveCountAsync()
            => await _repo.GetOnLeaveCountAsync();

        public async Task<IEnumerable<Attendance>> GetLast7DaysAsync()
            => await _repo.GetLast7DaysAllAsync();
    }
}

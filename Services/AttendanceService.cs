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

        public AttendanceService(ApplicationDbContext db)
        {
            _repo = new AttendanceRepository(db);
        }

        public async Task<IEnumerable<Attendance>> GetAttendanceAsync(int? employeeId, DateTime from, DateTime to)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null)
                throw new UnauthorizedAccessException();

            if (from.Date > to.Date)
                throw new InvalidOperationException("Khoảng ngày chấm công không hợp lệ.");

            if (user.Role == UserRole.Employee)
            {
                if (!user.EmployeeID.HasValue)
                    return new List<Attendance>();

                return await _repo.GetByEmployeeAsync(user.EmployeeID.Value, from.Date, to.Date);
            }

            if (employeeId.HasValue)
                return await _repo.GetByEmployeeAsync(employeeId.Value, from.Date, to.Date);

            return await _repo.GetAllByDateRangeAsync(from.Date, to.Date);
        }

        public async Task<bool> MarkAttendanceAsync(int employeeId, DateTime date, TimeSpan? checkIn, TimeSpan? checkOut, string status, double overtimeHours = 0, string note = null)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Manage_Attendance);

            if (employeeId <= 0)
                throw new InvalidOperationException("Nhân viên không hợp lệ.");

            if (checkIn.HasValue && checkOut.HasValue && checkOut.Value < checkIn.Value)
                throw new InvalidOperationException("Giờ ra phải lớn hơn hoặc bằng giờ vào.");

            if (overtimeHours < 0)
                throw new InvalidOperationException("Số giờ tăng ca không được âm.");

            var record = new Attendance
            {
                EmployeeID = employeeId,
                Date = date.Date,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Status = AttendanceStatuses.Normalize(status),
                OvertimeHours = overtimeHours,
                Note = note?.Trim()
            };

            await _repo.UpsertAsync(record);
            await AuditLogger.LogCreateAsync("Attendances", $"{employeeId}/{date:yyyy-MM-dd}", record.Status);
            return true;
        }

        public TimeSpan CalculateWorkingHours(TimeSpan? checkIn, TimeSpan? checkOut)
        {
            if (!checkIn.HasValue || !checkOut.HasValue)
                return TimeSpan.Zero;

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

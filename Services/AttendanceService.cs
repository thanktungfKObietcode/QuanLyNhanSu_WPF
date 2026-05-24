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
        private readonly EmployeeRepository _employeeRepo;

        public AttendanceService(ApplicationDbContext db)
        {
            _repo = new AttendanceRepository(db);
            _employeeRepo = new EmployeeRepository(db);
        }

        public async Task<IEnumerable<Attendance>> GetAttendanceAsync(int? employeeId, DateTime from, DateTime to)
        {
            var user = SessionManager.Instance.CurrentUser;
            if (user == null)
                throw new UnauthorizedAccessException();

            if (from.Date > to.Date)
                throw new InvalidOperationException("Khoang ngay cham cong khong hop le.");

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

        public async Task<(bool Success, string Error)> CreateAttendanceAsync(
            int employeeId,
            DateTime date,
            TimeSpan? checkIn,
            TimeSpan? checkOut,
            string status,
            double overtimeHours = 0,
            string note = null)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Manage_Attendance);

            var validationError = await ValidateAttendanceAsync(null, employeeId, date, checkIn, checkOut, status, overtimeHours);
            if (validationError != null)
                return (false, validationError);

            if (await _repo.ExistsForEmployeeAndDateAsync(employeeId, date))
                return (false, "Nhan vien nay da co ban ghi cham cong trong ngay duoc chon. Hay dung sua.");

            var record = BuildAttendance(0, employeeId, date, checkIn, checkOut, status, overtimeHours, note);
            await _repo.AddAsync(record);
            await AuditLogger.LogCreateAsync("Attendances", $"{employeeId}/{date:yyyy-MM-dd}", record.Status);
            return (true, null);
        }

        public async Task<(bool Success, string Error)> UpdateAttendanceAsync(
            int attendanceId,
            int employeeId,
            DateTime date,
            TimeSpan? checkIn,
            TimeSpan? checkOut,
            string status,
            double overtimeHours = 0,
            string note = null)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Manage_Attendance);

            if (attendanceId <= 0)
                return (false, "Ban ghi cham cong khong hop le.");

            var validationError = await ValidateAttendanceAsync(attendanceId, employeeId, date, checkIn, checkOut, status, overtimeHours);
            if (validationError != null)
                return (false, validationError);

            var record = BuildAttendance(attendanceId, employeeId, date, checkIn, checkOut, status, overtimeHours, note);
            await _repo.UpdateAsync(record);
            await AuditLogger.LogUpdateAsync("Attendances", attendanceId.ToString(), null, record.Status);
            return (true, null);
        }

        public async Task DeleteAttendanceAsync(int attendanceId)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Manage_Attendance);

            if (attendanceId <= 0)
                throw new InvalidOperationException("Ban ghi cham cong khong hop le.");

            await _repo.DeleteAsync(attendanceId);
            await AuditLogger.LogDeleteAsync("Attendances", attendanceId.ToString());
        }

        public async Task<bool> MarkAttendanceAsync(int employeeId, DateTime date, TimeSpan? checkIn, TimeSpan? checkOut, string status, double overtimeHours = 0, string note = null)
        {
            AuthorizationService.Current.CheckPermission(Permissions.Manage_Attendance);

            var validationError = await ValidateAttendanceAsync(null, employeeId, date, checkIn, checkOut, status, overtimeHours);
            if (validationError != null)
                throw new InvalidOperationException(validationError);

            var record = BuildAttendance(0, employeeId, date, checkIn, checkOut, status, overtimeHours, note);
            await _repo.UpsertAsync(record);
            await AuditLogger.LogCreateAsync("Attendances", $"{employeeId}/{date:yyyy-MM-dd}", record.Status);
            return true;
        }

        public double CalculateWorkingHours(TimeSpan? checkIn, TimeSpan? checkOut)
            => AttendanceCalculations.CalculateWorkingHours(checkIn, checkOut);

        public double CalculateWorkUnits(string status, TimeSpan? checkIn, TimeSpan? checkOut)
            => AttendanceCalculations.CalculateDailyWorkUnits(status, checkIn, checkOut);

        public async Task<double> GetMonthlyAttendanceRateAsync(int month, int year)
            => await _repo.GetMonthlyAttendanceRateAsync(month, year);

        public async Task<int> GetPresentTodayCountAsync()
            => await _repo.GetPresentTodayCountAsync();

        public async Task<int> GetOnLeaveCountAsync()
            => await _repo.GetOnLeaveCountAsync();

        public async Task<IEnumerable<Attendance>> GetLast7DaysAsync()
            => await _repo.GetLast7DaysAllAsync();

        private async Task<string> ValidateAttendanceAsync(
            int? attendanceId,
            int employeeId,
            DateTime date,
            TimeSpan? checkIn,
            TimeSpan? checkOut,
            string status,
            double overtimeHours)
        {
            if (employeeId <= 0)
                return "Nhan vien khong hop le.";

            var employee = await _employeeRepo.GetByIdAsync(employeeId);
            if (employee == null)
                return "Nhan vien duoc chon khong ton tai.";

            if (checkIn.HasValue && checkOut.HasValue && checkOut.Value < checkIn.Value)
                return "Gio ra phai lon hon hoac bang gio vao.";

            if (overtimeHours < 0)
                return "So gio tang ca khong duoc am.";

            var normalizedStatus = AttendanceStatuses.Normalize(status);
            if (string.IsNullOrWhiteSpace(normalizedStatus))
                return "Trang thai cham cong khong hop le.";

            if (await _repo.ExistsForEmployeeAndDateAsync(employeeId, date.Date, attendanceId))
                return "Nhan vien nay da co ban ghi cham cong trong ngay duoc chon.";

            return null;
        }

        private static Attendance BuildAttendance(
            int attendanceId,
            int employeeId,
            DateTime date,
            TimeSpan? checkIn,
            TimeSpan? checkOut,
            string status,
            double overtimeHours,
            string note)
        {
            return new Attendance
            {
                AttendanceID = attendanceId,
                EmployeeID = employeeId,
                Date = date.Date,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Status = AttendanceStatuses.Normalize(status),
                OvertimeHours = overtimeHours,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
            };
        }
    }
}

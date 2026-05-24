using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Repositories
{
    public class AttendanceRepository
    {
        private readonly ApplicationDbContext _db;

        public AttendanceRepository(ApplicationDbContext db) => _db = db;

        public async Task<Attendance> GetByIdAsync(int id)
            => await _db.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(a => a.AttendanceID == id);

        public async Task<IEnumerable<Attendance>> GetByEmployeeAsync(int employeeId, DateTime from, DateTime to)
        {
            var records = await _db.Attendances
                .Include(a => a.Employee)
                .Where(a => a.EmployeeID == employeeId && a.Date >= from && a.Date <= to)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            NormalizeStatuses(records);
            return records;
        }

        public async Task<IEnumerable<Attendance>> GetAllByDateRangeAsync(DateTime from, DateTime to)
        {
            var records = await _db.Attendances
                .Include(a => a.Employee).ThenInclude(e => e.Department)
                .Where(a => a.Date >= from && a.Date <= to)
                .ToListAsync();

            NormalizeStatuses(records);
            return records
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Employee?.Name ?? string.Empty)
                .ToList();
        }

        public async Task<Attendance> GetByEmployeeAndDateAsync(int employeeId, DateTime date)
        {
            var record = await _db.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.EmployeeID == employeeId && a.Date.Date == date.Date);

            if (record != null)
                record.Status = AttendanceStatuses.Normalize(record.Status);

            return record;
        }

        public Task<bool> ExistsForEmployeeAndDateAsync(int employeeId, DateTime date, int? excludeAttendanceId = null)
            => _db.Attendances.AnyAsync(a =>
                a.EmployeeID == employeeId &&
                a.Date.Date == date.Date &&
                (!excludeAttendanceId.HasValue || a.AttendanceID != excludeAttendanceId.Value));

        public async Task<int> GetPresentTodayCountAsync()
        {
            var today = DateTime.Today;
            return await _db.Attendances
                .CountAsync(a => a.Date.Date == today && AttendanceStatuses.PresentAliases.Contains(a.Status));
        }

        public async Task<int> GetOnLeaveCountAsync()
        {
            var today = DateTime.Today;
            return await _db.Attendances
                .CountAsync(a => a.Date.Date == today && AttendanceStatuses.OnLeaveAliases.Contains(a.Status));
        }

        public async Task<double> GetMonthlyAttendanceRateAsync(int month, int year)
        {
            var total = await _db.Attendances
                .CountAsync(a => a.Date.Month == month && a.Date.Year == year);
            if (total == 0)
                return 0;

            var present = await _db.Attendances
                .CountAsync(a => a.Date.Month == month && a.Date.Year == year
                    && (AttendanceStatuses.PresentAliases.Contains(a.Status)
                        || AttendanceStatuses.LateAliases.Contains(a.Status)));

            return Math.Round((double)present / total * 100, 1);
        }

        public async Task<IEnumerable<Attendance>> GetLast7DaysAllAsync()
        {
            var from = DateTime.Today.AddDays(-6);
            var records = await _db.Attendances
                .Where(a => a.Date >= from)
                .ToListAsync();

            NormalizeStatuses(records);
            return records;
        }

        public async Task AddAsync(Attendance attendance)
        {
            attendance.Employee = null;
            attendance.Status = AttendanceStatuses.Normalize(attendance.Status);
            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Attendance attendance)
        {
            var existing = await _db.Attendances.FindAsync(attendance.AttendanceID);
            if (existing == null)
                throw new InvalidOperationException($"Khong tim thay ban ghi cham cong co ID {attendance.AttendanceID}.");

            existing.EmployeeID = attendance.EmployeeID;
            existing.Date = attendance.Date.Date;
            existing.CheckIn = attendance.CheckIn;
            existing.CheckOut = attendance.CheckOut;
            existing.Status = AttendanceStatuses.Normalize(attendance.Status);
            existing.OvertimeHours = attendance.OvertimeHours;
            existing.Note = attendance.Note;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int attendanceId)
        {
            var existing = await _db.Attendances.FindAsync(attendanceId);
            if (existing == null)
                return;

            _db.Attendances.Remove(existing);
            await _db.SaveChangesAsync();
        }

        public async Task UpsertAsync(Attendance attendance)
        {
            attendance.Status = AttendanceStatuses.Normalize(attendance.Status);

            var existing = await GetByEmployeeAndDateAsync(attendance.EmployeeID, attendance.Date);
            if (existing == null)
            {
                _db.Attendances.Add(attendance);
            }
            else
            {
                existing.CheckIn = attendance.CheckIn;
                existing.CheckOut = attendance.CheckOut;
                existing.Status = attendance.Status;
                existing.OvertimeHours = attendance.OvertimeHours;
                existing.Note = attendance.Note;
            }

            await _db.SaveChangesAsync();
        }

        private static void NormalizeStatuses(IEnumerable<Attendance> records)
        {
            foreach (var record in records)
                record.Status = AttendanceStatuses.Normalize(record.Status);
        }
    }
}

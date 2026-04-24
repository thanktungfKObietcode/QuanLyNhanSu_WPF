using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Repositories
{
    public class AttendanceRepository
    {
        private readonly ApplicationDbContext _db;

        public AttendanceRepository(ApplicationDbContext db) => _db = db;

        public async Task<Attendance> GetByIdAsync(int id)
            => await _db.Attendances.FindAsync(id);

        public async Task<IEnumerable<Attendance>> GetByEmployeeAsync(int employeeId, DateTime from, DateTime to)
            => await _db.Attendances
                .Include(a => a.Employee)
                .Where(a => a.EmployeeID == employeeId && a.Date >= from && a.Date <= to)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

        public async Task<IEnumerable<Attendance>> GetAllByDateRangeAsync(DateTime from, DateTime to)
            => await _db.Attendances
                .Include(a => a.Employee).ThenInclude(e => e.Department)
                .Where(a => a.Date >= from && a.Date <= to)
                .OrderByDescending(a => a.Date).ThenBy(a => a.Employee.Name)
                .ToListAsync();

        public async Task<Attendance> GetByEmployeeAndDateAsync(int employeeId, DateTime date)
            => await _db.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeID == employeeId && a.Date.Date == date.Date);

        public async Task<int> GetPresentTodayCountAsync()
        {
            var today = DateTime.Today;
            return await _db.Attendances
                .CountAsync(a => a.Date.Date == today && a.Status == "Present");
        }

        public async Task<int> GetOnLeaveCountAsync()
        {
            var today = DateTime.Today;
            return await _db.Attendances
                .CountAsync(a => a.Date.Date == today && a.Status == "OnLeave");
        }

        public async Task<double> GetMonthlyAttendanceRateAsync(int month, int year)
        {
            var total = await _db.Attendances
                .CountAsync(a => a.Date.Month == month && a.Date.Year == year);
            if (total == 0) return 0;
            var present = await _db.Attendances
                .CountAsync(a => a.Date.Month == month && a.Date.Year == year
                              && (a.Status == "Present" || a.Status == "Late"));
            return Math.Round((double)present / total * 100, 1);
        }

        public async Task<IEnumerable<Attendance>> GetLast7DaysAllAsync()
        {
            var from = DateTime.Today.AddDays(-6);
            return await _db.Attendances
                .Where(a => a.Date >= from)
                .ToListAsync();
        }

        public async Task AddAsync(Attendance attendance)
        {
            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Attendance attendance)
        {
            _db.Attendances.Update(attendance);
            await _db.SaveChangesAsync();
        }

        public async Task UpsertAsync(Attendance attendance)
        {
            var existing = await GetByEmployeeAndDateAsync(attendance.EmployeeID, attendance.Date);
            if (existing == null)
                _db.Attendances.Add(attendance);
            else
            {
                existing.CheckIn = attendance.CheckIn;
                existing.CheckOut = attendance.CheckOut;
                existing.Status = attendance.Status;
                existing.Note = attendance.Note;
            }
            await _db.SaveChangesAsync();
        }
    }
}

using System;

namespace QuanLyNhanSu_WPF.Models
{
    public class DailySalaryEntry
    {
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string DepartmentName { get; set; }
        public DateTime Date { get; set; }
        public string AttendanceStatus { get; set; }
        public double WorkUnits { get; set; }
        public double WorkingHours { get; set; }
        public double OvertimeHours { get; set; }
        public decimal DailyRate { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal OvertimeAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Note { get; set; }
    }
}

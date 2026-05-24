using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhanSu_WPF.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceID { get; set; }

        public int EmployeeID { get; set; }
        [ForeignKey("EmployeeID")]
        public Employee Employee { get; set; }

        public DateTime Date { get; set; }

        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } // Present, Absent, Late, OnLeave

        [MaxLength(500)]
        public string Note { get; set; }

        public double OvertimeHours { get; set; }

        [NotMapped]
        public double WorkingHours => Helpers.AttendanceCalculations.CalculateWorkingHours(CheckIn, CheckOut);

        [NotMapped]
        public double WorkUnits => Helpers.AttendanceCalculations.CalculateDailyWorkUnits(Status, CheckIn, CheckOut);
    }
}

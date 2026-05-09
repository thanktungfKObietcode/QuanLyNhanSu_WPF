using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhanSu_WPF.Models
{
    public class Salary
    {
        [Key]
        public int SalaryID { get; set; }

        public int EmployeeID { get; set; }
        [ForeignKey("EmployeeID")]
        public Employee Employee { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        public decimal BaseSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal KPIBonus { get; set; }
        public decimal OTSalary { get; set; }
        public decimal Commission { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }

        public DateTime? PaidDate { get; set; }
    }
}
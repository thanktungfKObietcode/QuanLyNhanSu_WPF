using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhanSu_WPF.Models
{
    public enum EmployeeStatus
    {
        Active,
        Inactive,
        OnLeave
    }

    // Employment type determines salary factor
    public enum EmploymentType
    {
        Permanent,   // Full salary
        Probation,   // 70% of base salary
        PartTime     // 50% of base salary
    }

    public class Employee
    {
        [Key]
        public int EmployeeID { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public DateTime DateOfBirth { get; set; }

        [MaxLength(10)]
        public string Gender { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(200)]
        public string Email { get; set; }

        [MaxLength(500)]
        public string Address { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Today;

        public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
        public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;
        // Total experience in days (e.g., previous jobs, internships)
        public int ExperienceDays { get; set; }
        // Highest degree or certification
        public string Degree { get; set; }

        public int? DepartmentID { get; set; }
        [ForeignKey("DepartmentID")]
        public Department Department { get; set; }

        public int? PositionID { get; set; }
        [ForeignKey("PositionID")]
        public Position Position { get; set; }

        [MaxLength(500)]
        public string Photo { get; set; }

        // Mức hoa hồng cơ bản (%) cho nhân viên Sales
        public double BaseCommissionRate { get; set; } = 0;
    }
}

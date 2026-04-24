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

        public int? DepartmentID { get; set; }
        [ForeignKey("DepartmentID")]
        public Department Department { get; set; }

        public int? PositionID { get; set; }
        [ForeignKey("PositionID")]
        public Position Position { get; set; }

        [MaxLength(500)]
        public string Photo { get; set; }
    }
}

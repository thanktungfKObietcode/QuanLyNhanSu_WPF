using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhanSu_WPF.Models
{
    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public class LeaveRequest
    {
        [Key]
        public int LeaveRequestID { get; set; }

        public int EmployeeID { get; set; }
        [ForeignKey("EmployeeID")]
        public Employee Employee { get; set; }

        [Required]
        [MaxLength(100)]
        public string LeaveType { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [MaxLength(1000)]
        public string Reason { get; set; }

        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        public int? ApprovedByUserID { get; set; }
        [ForeignKey("ApprovedByUserID")]
        public User ApprovedByUser { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [MaxLength(500)]
        public string ApprovalNote { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

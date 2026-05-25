using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhanSu_WPF.Models
{
    public class EmailLog
    {
        [Key]
        public int LogID { get; set; }

        public int? EmployeeID { get; set; }
        
        [ForeignKey("EmployeeID")]
        public Employee Employee { get; set; }

        [Required]
        [MaxLength(255)]
        public string EmailAddress { get; set; }

        [Required]
        [MaxLength(255)]
        public string Subject { get; set; }

        [MaxLength(50)]
        public string EmailType { get; set; } // e.g. "Birthday", "Manual"

        public DateTime SentDate { get; set; } = DateTime.Now;

        public bool IsSuccess { get; set; }
        
        public string ErrorMessage { get; set; }
    }
}

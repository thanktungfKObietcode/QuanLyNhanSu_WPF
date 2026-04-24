using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyNhanSu_WPF.Models
{
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }

        [MaxLength(50)]
        public string DeptCode { get; set; }

        [MaxLength(200)]
        public string DeptName { get; set; }

        public int? ManagerID { get; set; }
        [ForeignKey("ManagerID")]
        public Employee Manager { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace QuanLyNhanSu_WPF.Models
{
    public class Position
    {
        [Key]
        public int PositionID { get; set; }

        [MaxLength(50)]
        public string PosCode { get; set; }

        [MaxLength(200)]
        public string PosName { get; set; }

        public decimal BaseSalary { get; set; }
    }
}

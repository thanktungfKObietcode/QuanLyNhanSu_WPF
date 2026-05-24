namespace QuanLyNhanSu_WPF.Models
{
    public class MonthlySalarySummary
    {
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string DepartmentName { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public double TotalWorkUnits { get; set; }
        public double TotalWorkingHours { get; set; }
        public double TotalOvertimeHours { get; set; }
        public decimal DailySalaryTotal { get; set; }
        public decimal OvertimeSalaryTotal { get; set; }
        public decimal FixedAllowances { get; set; }
        public decimal ProjectedNetSalary { get; set; }
        public decimal? OfficialNetSalary { get; set; }
        public decimal? OfficialBaseSalary { get; set; }
    }
}

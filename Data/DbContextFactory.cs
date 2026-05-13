using Microsoft.EntityFrameworkCore;

namespace QuanLyNhanSu_WPF.Data
{
    public class DbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            string connectionString = "Server=.\\SQLEXPRESS;Database=QuanLyNhanSuDB;Trusted_Connection=True;TrustServerCertificate=True;";
            optionsBuilder.UseSqlServer(connectionString);
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        public static DbContextOptions<ApplicationDbContext> CreateOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            string connectionString = "Server=.\\SQLEXPRESS;Database=QuanLyNhanSuDB;Trusted_Connection=True;TrustServerCertificate=True;";
            optionsBuilder.UseSqlServer(connectionString);
            return optionsBuilder.Options;
        }
    }
}

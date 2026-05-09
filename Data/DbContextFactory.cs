using Microsoft.EntityFrameworkCore;

namespace QuanLyNhanSu_WPF.Data
{
    public class DbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var basePath = System.IO.Directory.GetCurrentDirectory();
            var dbPath = System.IO.Path.Combine(basePath, "app_data", "qlns.db");
            var dir = System.IO.Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        public static DbContextOptions<ApplicationDbContext> CreateOptions()
        {
            var basePath = System.AppDomain.CurrentDomain.BaseDirectory;
            var dbPath = System.IO.Path.Combine(basePath, "app_data", "qlns.db");
            var dir = System.IO.Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir))
                System.IO.Directory.CreateDirectory(dir);

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            return optionsBuilder.Options;
        }
    }
}

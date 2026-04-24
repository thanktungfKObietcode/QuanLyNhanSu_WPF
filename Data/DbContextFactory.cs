using Microsoft.EntityFrameworkCore;

namespace QuanLyNhanSu_WPF.Data
{
    public static class DbContextFactory
    {
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

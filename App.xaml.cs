using System.Windows;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;
using System.Linq;

namespace QuanLyNhanSu_WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global Exception Handling
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Use SQLite local database file
            var basePath = System.AppDomain.CurrentDomain.BaseDirectory;
            var dbPath = System.IO.Path.Combine(basePath, "app_data", "qlns.db");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");

            using var db = new ApplicationDbContext(optionsBuilder.Options);

            // Apply any pending migrations (creates database if it doesn't exist)
            db.Database.Migrate();

            // Đảm bảo schema mới cho công ty truyền thông (Thêm cột nếu chưa có)
            try { db.Database.ExecuteSqlRaw("ALTER TABLE Salaries ADD COLUMN KPIBonus REAL DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE Salaries ADD COLUMN OTSalary REAL DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE Salaries ADD COLUMN Commission REAL DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE Attendances ADD COLUMN OvertimeHours REAL DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE Employees ADD COLUMN BaseCommissionRate REAL DEFAULT 0"); } catch { }

            // Create or reset default admin user
            var admin = db.Users.FirstOrDefault(u => u.Username == "admin");
            PasswordHasher.HashPassword("admin123", out var hash, out var salt);
            
            if (admin == null)
            {
                admin = new User
                {
                    Username = "admin",
                    PasswordHash = hash,
                    Salt = salt,
                    Role = UserRole.Admin,
                    IsActive = true,
                    LastLogin = null
                };
                db.Users.Add(admin);
            }
            else
            {
                // Reset password and unlock account
                admin.PasswordHash = hash;
                admin.Salt = salt;
                admin.FailedLoginAttempts = 0;
                admin.LockoutEnd = null;
                db.Users.Update(admin);
            }
            db.SaveChanges();

            // Seed các chức vụ ngành truyền thông nếu chưa có
            if (!db.Positions.Any(p => p.PosName == "Sale Media"))
            {
                db.Positions.AddRange(
                    new Position { PosName = "Sale Media", PosCode = "SALE_MEDIA", BaseSalary = 8000000 },
                    new Position { PosName = "Content Creator", PosCode = "CONTENT", BaseSalary = 10000000 },
                    new Position { PosName = "Graphic Designer", PosCode = "DESIGN", BaseSalary = 12000000 },
                    new Position { PosName = "Account Manager", PosCode = "ACCOUNT", BaseSalary = 15000000 },
                    new Position { PosName = "Video Editor", PosCode = "EDITOR", BaseSalary = 11000000 }
                );
                db.SaveChanges();
            }

            ShowLoginWindow();
        }

        private void ShowLoginWindow()
        {
            var loginView = new Views.LoginView();
            var vm = new ViewModels.LoginViewModel();
            
            vm.LoginSucceeded += () =>
            {
                var mainWindow = new MainWindow();
                var mainVm = new ViewModels.MainViewModel();
                
                mainVm.LogoutRequested += () =>
                {
                    mainWindow.Close();
                    ShowLoginWindow();
                };
                
                mainWindow.DataContext = mainVm;
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                loginView.Close();
            };

            loginView.DataContext = vm;
            loginView.Show();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the error (could use a logging framework here)
            System.Diagnostics.Debug.WriteLine($"CRITICAL ERROR: {e.Exception}");
            
            MessageBox.Show("Đã xảy ra lỗi không mong muốn. Ứng dụng sẽ được ghi lại nhật ký lỗi.", 
                            "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Prevent application from crashing
            e.Handled = true;
        }
    }
}

using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Helpers;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connectionString = "Server=.\\SQLEXPRESS;Database=QuanLyNhanSuDB;Trusted_Connection=True;TrustServerCertificate=True;";
            optionsBuilder.UseSqlServer(connectionString);

            using var db = new ApplicationDbContext(optionsBuilder.Options);

            db.Database.Migrate();

            // Backfill columns safely for older SQL Server databases that may predate the current model.
            try { db.Database.ExecuteSqlRaw("IF COL_LENGTH('Salaries', 'KPIBonus') IS NULL ALTER TABLE Salaries ADD KPIBonus decimal(18,2) NOT NULL CONSTRAINT DF_Salaries_KPIBonus DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("IF COL_LENGTH('Salaries', 'OTSalary') IS NULL ALTER TABLE Salaries ADD OTSalary decimal(18,2) NOT NULL CONSTRAINT DF_Salaries_OTSalary DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("IF COL_LENGTH('Salaries', 'Commission') IS NULL ALTER TABLE Salaries ADD Commission decimal(18,2) NOT NULL CONSTRAINT DF_Salaries_Commission DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("IF COL_LENGTH('Attendances', 'OvertimeHours') IS NULL ALTER TABLE Attendances ADD OvertimeHours float NOT NULL CONSTRAINT DF_Attendances_OvertimeHours DEFAULT 0"); } catch { }
            try { db.Database.ExecuteSqlRaw("IF COL_LENGTH('Employees', 'BaseCommissionRate') IS NULL ALTER TABLE Employees ADD BaseCommissionRate float NOT NULL CONSTRAINT DF_Employees_BaseCommissionRate DEFAULT 0"); } catch { }

            var admin = db.Users.FirstOrDefault(u => u.Username == "admin");
            PasswordHasher.HashPassword("Admin@123", out var hash, out var salt);

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

            db.SaveChanges();

            if (!db.Positions.Any())
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
            bool loginSucceeded = false;

            vm.LoginSucceeded += () =>
            {
                loginSucceeded = true;
                var mainWindow = new MainWindow();
                var mainVm = new ViewModels.MainViewModel();

                // If user closes MainWindow via X button (not logout), shut down the app
                bool isLoggingOut = false;

                mainVm.LogoutRequested += () =>
                {
                    isLoggingOut = true;
                    mainWindow.Close();
                    ShowLoginWindow();
                };

                mainWindow.Closed += (s, args) =>
                {
                    if (!isLoggingOut)
                    {
                        Current.Shutdown();
                    }
                };

                mainWindow.DataContext = mainVm;
                Current.MainWindow = mainWindow;
                mainWindow.Show();
                loginView.Close();
            };

            // If login window is closed without successful login, shut down the app
            loginView.Closed += (s, args) =>
            {
                if (!loginSucceeded)
                {
                    Current.Shutdown();
                }
            };

            loginView.DataContext = vm;
            loginView.Show();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"CRITICAL ERROR: {e.Exception}");

            MessageBox.Show(
                "Đã xảy ra lỗi không mong muốn. Ứng dụng sẽ được ghi lại nhật ký lỗi.",
                "Lỗi hệ thống",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}

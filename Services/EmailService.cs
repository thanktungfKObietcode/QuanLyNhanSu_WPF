using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Services
{
    public class EmailService
    {

        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string SmtpUser = "thanhtungg28686@gmail.com";
        private const string SmtpPass = "dorc jwvt umdd wvjp"; 

        private readonly ApplicationDbContext _db;

        public EmailService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(bool success, string error)> SendEmailAsync(string toAddress, string subject, string body, int? employeeId = null, string emailType = "Manual")
        {
            if (string.IsNullOrEmpty(SmtpUser) || SmtpUser == "your_email@gmail.com")
            {
                return (false, "Vui lòng cấu hình tài khoản Email (SmtpUser, SmtpPass) trong EmailService.cs");
            }

            var log = new EmailLog
            {
                EmployeeID = employeeId,
                EmailAddress = toAddress,
                Subject = subject,
                EmailType = emailType,
                SentDate = DateTime.Now
            };

            try
            {
                using var client = new SmtpClient(SmtpServer, SmtpPort)
                {
                    Credentials = new NetworkCredential(SmtpUser, SmtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(SmtpUser, "HR Manager"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toAddress);

                await client.SendMailAsync(mailMessage);

                log.IsSuccess = true;
                _db.EmailLogs.Add(log);
                await _db.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                log.IsSuccess = false;
                log.ErrorMessage = ex.Message;
                _db.EmailLogs.Add(log);
                await _db.SaveChangesAsync();

                return (false, ex.Message);
            }
        }

        public async Task<int> SendBirthdayEmailsAsync()
        {
            int sentCount = 0;
            var today = DateTime.Today;

            var employees = await _db.Employees
                .Where(e => e.Status == EmployeeStatus.Active && !string.IsNullOrEmpty(e.Email))
                .ToListAsync();

            var birthdayEmployees = employees.Where(e => 
                e.DateOfBirth.Day == today.Day && 
                e.DateOfBirth.Month == today.Month).ToList();

            if (!birthdayEmployees.Any()) return 0;

            // Check who already received a birthday email this year
            var alreadySentLogs = await _db.EmailLogs
                .Where(log => log.EmailType == "Birthday" && log.SentDate.Year == today.Year)
                .Select(log => log.EmployeeID)
                .ToListAsync();

            foreach (var emp in birthdayEmployees)
            {
                if (alreadySentLogs.Contains(emp.EmployeeID))
                    continue; // Already sent

                string subject = $"🎂 Chúc Mừng Sinh Nhật {emp.Name}!";
                string body = $"<h2>Chúc Mừng Sinh Nhật!</h2>" +
                              $"<p>Chào {emp.Name},</p>" +
                              $"<p>Thay mặt công ty, xin gửi đến bạn những lời chúc tốt đẹp nhất nhân ngày sinh nhật. " +
                              $"Chúc bạn luôn mạnh khỏe, hạnh phúc và gặt hái được nhiều thành công trong tuổi mới!</p>" +
                              $"<br><p>Trân trọng,<br>Phòng Nhân Sự</p>";

                var result = await SendEmailAsync(emp.Email, subject, body, emp.EmployeeID, "Birthday");
                if (result.success)
                {
                    sentCount++;
                }
            }

            return sentCount;
        }
    }
}

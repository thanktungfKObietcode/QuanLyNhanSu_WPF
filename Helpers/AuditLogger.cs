using System;
using System.Threading.Tasks;
using QuanLyNhanSu_WPF.Data;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Helpers
{
    /// <summary>
    /// Centralized audit logging service.
    /// All CRUD operations, login events, and security violations should be logged here.
    /// </summary>
    public static class AuditLogger
    {
        /// <summary>Log any action. Call from Services after DB operations.</summary>
        public static async Task LogAsync(
            string action,
            string tableName = null,
            string recordId = null,
            string oldValue = null,
            string newValue = null)
        {
            try
            {
                var currentUser = SessionManager.Instance.CurrentUser;
                var log = new AuditLog
                {
                    UserID = currentUser?.UserID,
                    Action = action,
                    TableName = tableName,
                    RecordID = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Timestamp = DateTime.UtcNow
                };

                var options = DbContextFactory.CreateOptions();
                await using var db = new ApplicationDbContext(options);
                db.AuditLogs.Add(log);
                await db.SaveChangesAsync();
            }
            catch
            {
                // Audit logging must never crash the application
            }
        }

        public static Task LogLoginAsync(string username)
            => LogAsync($"LOGIN: {username}", "Users", null);

        public static Task LogLogoutAsync(string username)
            => LogAsync($"LOGOUT: {username}", "Users", null);

        public static Task LogPermissionDeniedAsync(string permission)
            => LogAsync($"PERMISSION_DENIED: {permission}");

        public static Task LogExportAsync(string dataType)
            => LogAsync($"EXPORT: {dataType}");

        public static Task LogCreateAsync(string table, string recordId, string newValue = null)
            => LogAsync($"CREATE", table, recordId, null, newValue);

        public static Task LogUpdateAsync(string table, string recordId, string oldValue, string newValue)
            => LogAsync($"UPDATE", table, recordId, oldValue, newValue);

        public static Task LogDeleteAsync(string table, string recordId, string oldValue = null)
            => LogAsync($"DELETE", table, recordId, oldValue, null);
    }
}

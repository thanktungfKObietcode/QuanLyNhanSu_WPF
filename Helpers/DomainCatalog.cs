using System;
using System.Collections.Generic;

namespace QuanLyNhanSu_WPF.Helpers
{
    public static class AttendanceStatuses
    {
        public const string Present = "Có mặt";
        public const string Late = "Đi muộn";
        public const string Absent = "Vắng mặt";
        public const string OnLeave = "Nghỉ phép";

        public static readonly string[] PresentAliases = { Present, "CÃ³ máº·t", "Present" };
        public static readonly string[] LateAliases = { Late, "Äi muá»™n", "Late" };
        public static readonly string[] AbsentAliases = { Absent, "Váº¯ng máº·t", "Absent" };
        public static readonly string[] OnLeaveAliases = { OnLeave, "Nghá»‰ phÃ©p", "OnLeave" };

        public static IReadOnlyList<string> AllDisplayValues { get; } = new[]
        {
            Present,
            Late,
            Absent,
            OnLeave
        };

        public static string Normalize(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return Absent;

            if (Contains(PresentAliases, status))
                return Present;

            if (Contains(LateAliases, status))
                return Late;

            if (Contains(AbsentAliases, status))
                return Absent;

            if (Contains(OnLeaveAliases, status))
                return OnLeave;

            return status.Trim();
        }

        public static bool IsPresentOrLate(string status)
        {
            var normalized = Normalize(status);
            return normalized == Present || normalized == Late;
        }

        public static bool IsPaidWorkingDay(string status)
        {
            var normalized = Normalize(status);
            return normalized == Present || normalized == Late || normalized == OnLeave;
        }

        private static bool Contains(IEnumerable<string> values, string candidate)
        {
            foreach (var value in values)
            {
                if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    public static class LeaveTypes
    {
        public const string Annual = "Annual";
        public const string Sick = "Sick";
        public const string Maternity = "Maternity";
        public const string Unpaid = "Unpaid";

        public static IReadOnlyList<string> All { get; } = new[]
        {
            Annual,
            Sick,
            Maternity,
            Unpaid
        };
    }
}

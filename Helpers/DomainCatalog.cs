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

        public static readonly string[] PresentAliases = { Present, "Có mặt", "Present" };
        public static readonly string[] LateAliases = { Late, "Đi muộn", "Late" };
        public static readonly string[] AbsentAliases = { Absent, "Vắng mặt", "Absent" };
        public static readonly string[] OnLeaveAliases = { OnLeave, "Nghỉ phép", "OnLeave" };

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

    public static class AttendanceCalculations
    {
        private const double StandardHoursPerDay = 8d;
        private const double HalfDayThresholdHours = 4d;

        public static double CalculateWorkingHours(TimeSpan? checkIn, TimeSpan? checkOut)
        {
            if (!checkIn.HasValue || !checkOut.HasValue || checkOut.Value <= checkIn.Value)
                return 0;

            return Math.Round((checkOut.Value - checkIn.Value).TotalHours, 2);
        }

        public static double CalculateDailyWorkUnits(string status, TimeSpan? checkIn, TimeSpan? checkOut)
        {
            var normalized = AttendanceStatuses.Normalize(status);

            if (normalized == AttendanceStatuses.Absent)
                return 0;

            if (normalized == AttendanceStatuses.OnLeave)
                return 1;

            var workingHours = CalculateWorkingHours(checkIn, checkOut);
            if (workingHours <= 0)
                return normalized == AttendanceStatuses.Present || normalized == AttendanceStatuses.Late ? 1 : 0;

            if (workingHours < HalfDayThresholdHours)
                return 0.5;

            if (workingHours < StandardHoursPerDay)
                return 0.75;

            return 1;
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

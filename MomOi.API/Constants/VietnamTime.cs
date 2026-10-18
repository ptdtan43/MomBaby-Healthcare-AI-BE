using System;

namespace MomOi.API.Constants
{
    /// <summary>
    /// Calendar dates for users in Vietnam. Vietnam has no daylight saving, so a fixed
    /// UTC+7 offset is exact and avoids depending on tzdata being present in the container.
    ///
    /// Two kinds of columns exist and must not be mixed:
    /// - date labels (DailyMonitoringLog.Date, LifestyleEntry.Date, ...) store a calendar day
    ///   as midnight-UTC: compare them with <see cref="Today"/>.
    /// - instants (LoggedAt, RecordedAt, ...) store a real UTC moment: filter them with the
    ///   range [<see cref="TodayStartUtc"/>, TodayStartUtc + 1 day).
    /// </summary>
    public static class VietnamTime
    {
        private static readonly TimeSpan Offset = TimeSpan.FromHours(7);

        public static DateTime Today => DateTime.UtcNow.Add(Offset).Date;

        /// <summary>The UTC instant at which today began in Vietnam (00:00 UTC+7).</summary>
        public static DateTime TodayStartUtc => Today - Offset;

        /// <summary>The Vietnamese calendar day an UTC instant falls on.</summary>
        public static DateTime LocalDate(DateTime utcInstant) => utcInstant.Add(Offset).Date;

        /// <summary>Giờ (0-23) theo giờ Việt Nam của một mốc UTC.</summary>
        public static int LocalHour(DateTime utcInstant) => utcInstant.Add(Offset).Hour;
    }
}

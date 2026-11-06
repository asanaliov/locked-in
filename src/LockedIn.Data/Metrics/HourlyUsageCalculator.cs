namespace LockedIn.Data.Metrics;

public static class HourlyUsageCalculator
{
    /// <summary>Minutes of screen time in each local hour of one day. Sessions must already be clipped to that day.</summary>
    /// <param name="zone">The local time zone by default.</param>
    public static double[] MinutesPerHour(IEnumerable<UsageSession> sessions, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Local;
        var minutes = new double[24];
        foreach (var session in sessions)
        {
            // Walks real (UTC) time and only looks up the local hour, because local times jump when the clocks change.
            var start = session.StartTime;
            while (start < session.EndTime)
            {
                var local = TimeZoneInfo.ConvertTime(start, zone);
                var nextHour = start + TimeSpan.FromHours(local.Hour + 1) - local.TimeOfDay;
                var sliceEnd = session.EndTime < nextHour ? session.EndTime : nextHour;
                minutes[local.Hour] += (sliceEnd - start).TotalMinutes;
                start = sliceEnd;
            }
        }
        return minutes;
    }
}

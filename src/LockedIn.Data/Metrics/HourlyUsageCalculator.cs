namespace LockedIn.Data.Metrics;

public static class HourlyUsageCalculator
{
    /// <summary>Minutes of screen time in each local hour of one day. Sessions must already be clipped to that day.</summary>
    public static double[] MinutesPerHour(IEnumerable<UsageSession> sessions)
    {
        var minutes = new double[24];
        foreach (var session in sessions)
        {
            var start = session.StartTime.ToLocalTime();
            var end = session.EndTime.ToLocalTime();

            // A session that runs past the hour mark is split between both hours.
            while (start < end)
            {
                var nextHour = start.Date.AddHours(start.Hour + 1);
                var sliceEnd = end < nextHour ? end : nextHour;
                minutes[start.Hour] += (sliceEnd - start).TotalMinutes;
                start = sliceEnd;
            }
        }
        return minutes;
    }
}

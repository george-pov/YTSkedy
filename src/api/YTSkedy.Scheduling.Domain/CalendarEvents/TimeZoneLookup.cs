namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public static class TimeZoneLookup
{
    public static bool TryFind(string? timeZoneId, out TimeZoneInfo timeZone)
    {
        timeZone = default!;

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        if (TryFindDirect(timeZoneId, out timeZone))
        {
            return true;
        }

        return TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsTimeZoneId) &&
            TryFindDirect(windowsTimeZoneId, out timeZone);
    }

    private static bool TryFindDirect(string timeZoneId, out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (Exception exception)
            when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            timeZone = default!;
            return false;
        }
    }
}

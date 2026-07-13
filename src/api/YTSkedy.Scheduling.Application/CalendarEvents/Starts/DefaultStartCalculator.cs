using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents.Starts;

public static class DefaultStartCalculator
{
    public static DefaultStart Calculate(
        StartDefaults defaults,
        string? fallbackTimeZoneId,
        IReadOnlySet<DateTimeOffset> occupiedStarts,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        ArgumentNullException.ThrowIfNull(occupiedStarts);

        var timeZoneId = SelectTimeZoneId(defaults, fallbackTimeZoneId);
        if (defaults.DayOfWeek is null || timeZoneId is null)
        {
            return new DefaultStart(null, defaults.LocalTime, timeZoneId);
        }

        if (!TimeZoneLookup.TryFind(timeZoneId, out var timeZone))
        {
            return new DefaultStart(null, defaults.LocalTime, null);
        }

        var currentLocalDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        var date = FindFirstDate(currentLocalDate, defaults.DayOfWeek.Value);

        if (defaults.LocalTime is null)
        {
            return new DefaultStart(date, null, timeZoneId);
        }

        while (!IsAvailable(date, defaults.LocalTime.Value, timeZoneId, occupiedStarts, now))
        {
            date = date.AddDays(7);
        }

        return new DefaultStart(date, defaults.LocalTime, timeZoneId);
    }

    private static string? SelectTimeZoneId(
        StartDefaults defaults,
        string? fallbackTimeZoneId) =>
        defaults.TimeZoneId ?? fallbackTimeZoneId;

    private static DateOnly FindFirstDate(DateOnly currentDate, DayOfWeek targetDay)
    {
        var daysUntilTarget = ((int)targetDay - (int)currentDate.DayOfWeek + 7) % 7;
        return currentDate.AddDays(daysUntilTarget);
    }

    private static bool IsAvailable(
        DateOnly date,
        TimeOnly time,
        string timeZoneId,
        IReadOnlySet<DateTimeOffset> occupiedStarts,
        DateTimeOffset now)
    {
        try
        {
            var conversion = ScheduledStartConverter.Convert(
                new ScheduledStart(date.ToDateTime(time), timeZoneId));

            return conversion.ScheduledStartUtc > now &&
                !occupiedStarts.Contains(conversion.ScheduledStartUtc);
        }
        catch (InvalidScheduledStartException)
        {
            return false;
        }
    }
}

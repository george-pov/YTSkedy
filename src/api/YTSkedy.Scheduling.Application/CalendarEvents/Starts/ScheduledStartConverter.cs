using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents.Starts;

public static class ScheduledStartConverter
{
    public static ScheduledStartConversion Convert(ScheduledStart start)
    {
        ArgumentNullException.ThrowIfNull(start);

        if (string.IsNullOrWhiteSpace(start.TimeZoneId))
        {
            throw new InvalidScheduledStartException(
                "Start local date-time and time zone id are required.");
        }

        var localDateTime = DateTime.SpecifyKind(
            start.LocalDateTime,
            DateTimeKind.Unspecified);
        if (!TimeZoneLookup.TryFind(start.TimeZoneId, out var timeZone))
        {
            throw new InvalidScheduledStartException(
                "Time zone id is not recognized.");
        }

        if (timeZone.IsInvalidTime(localDateTime))
        {
            throw new InvalidScheduledStartException(
                "Scheduled start time does not exist in the specified time zone.");
        }

        if (timeZone.IsAmbiguousTime(localDateTime))
        {
            throw new InvalidScheduledStartException(
                "Scheduled start time is ambiguous in the specified time zone.");
        }

        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);

        return new ScheduledStartConversion(new DateTimeOffset(utcDateTime, TimeSpan.Zero));
    }
}

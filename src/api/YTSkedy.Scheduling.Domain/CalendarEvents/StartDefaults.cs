namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record StartDefaults
{
    public StartDefaults(
        DayOfWeek? dayOfWeek,
        TimeOnly? localTime,
        string? timeZoneId)
    {
        if (timeZoneId is not null && !TimeZoneLookup.TryFind(timeZoneId, out _))
        {
            throw new ArgumentException(
                "Time zone id must be a recognized non-blank value.",
                nameof(timeZoneId));
        }

        DayOfWeek = dayOfWeek;
        LocalTime = localTime;
        TimeZoneId = timeZoneId;
    }

    public static StartDefaults Empty { get; } = new(null, null, null);

    public DayOfWeek? DayOfWeek { get; }

    public TimeOnly? LocalTime { get; }

    public string? TimeZoneId { get; }
}

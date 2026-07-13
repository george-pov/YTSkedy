using YTSkedy.Scheduling.Application.Settings;

namespace YTSkedy.Scheduling.Application.CalendarEvents.Starts;

public sealed class GetDefaultStartHandler(
    IStartDefaultsReader startDefaults,
    ICalendarEventReader calendarEvents,
    TimeProvider timeProvider)
{
    public async Task<DefaultStart> HandleAsync(
        string? fallbackTimeZoneId,
        CancellationToken cancellationToken)
    {
        var defaults = await startDefaults.GetAsync(cancellationToken);
        var events = await calendarEvents.ListAsync(null, cancellationToken);
        var occupiedStarts = events
            .Select(record => record.Event.ScheduledStartUtc)
            .ToHashSet();

        return DefaultStartCalculator.Calculate(
            defaults,
            fallbackTimeZoneId,
            occupiedStarts,
            timeProvider.GetUtcNow());
    }
}

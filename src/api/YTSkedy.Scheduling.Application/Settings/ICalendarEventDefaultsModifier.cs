namespace YTSkedy.Scheduling.Application.Settings;

public interface ICalendarEventDefaultsModifier
{
    Task SaveAsync(
        CalendarEventDefaults defaults,
        CancellationToken cancellationToken);
}

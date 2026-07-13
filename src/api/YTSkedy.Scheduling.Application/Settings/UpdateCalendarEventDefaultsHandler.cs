using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public sealed class UpdateCalendarEventDefaultsHandler(
    ICalendarEventDefaultsModifier defaults)
{
    public async Task<CalendarEventDefaults> HandleAsync(
        UpdateCalendarEventDefaultsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var replacement = new CalendarEventDefaults(
            new EventTextFields(command.Fields),
            new StartDefaults(
                command.DayOfWeek,
                command.LocalTime,
                command.TimeZoneId));

        await defaults.SaveAsync(replacement, cancellationToken);

        return replacement;
    }
}

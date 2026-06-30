using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class UpdateCalendarEventHandler(
    ICalendarEventReader calendarEventReader,
    ICalendarEventModifier calendarEvents)
{
    public async Task<UpdateCalendarEventResult> HandleAsync(
        UpdateEventTextCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = await calendarEventReader.GetByIdAsync(
            command.CalendarEventId,
            cancellationToken);

        if (calendarEvent is null)
        {
            return UpdateCalendarEventResult.NotFound;
        }

        var text = calendarEvent.Text.UpdateValues(command.Texts);
        var updated = await calendarEvents.UpdateTextAsync(
            command.CalendarEventId,
            text,
            cancellationToken);

        // The row was read but vanished before the write. Treat the
        // stale client as a missing event so the host returns 404.
        return updated
            ? UpdateCalendarEventResult.Updated
            : UpdateCalendarEventResult.NotFound;
    }
}

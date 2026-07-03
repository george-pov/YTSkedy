using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class UpdateCalendarEventHandler(
    ICalendarEventReader calendarEventReader,
    IPlatformPublicationReader publicationReader,
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

        var publicationRows = await publicationReader.ListByEventAsync(
            command.CalendarEventId,
            cancellationToken);
        if (publicationRows.Count > 0)
        {
            return UpdateCalendarEventResult.HasPlatformPublications;
        }

        EventTextSnapshot text;
        try
        {
            text = calendarEvent.Text.UpdateValues(command.Texts);
        }
        catch (ArgumentException exception)
        {
            return UpdateCalendarEventResult.Invalid(exception.Message);
        }

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

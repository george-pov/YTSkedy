using YTSkedy.Scheduling.Application.CalendarEvents.Starts;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class UpdateCalendarEventHandler(
    ICalendarEventReader calendarEventReader,
    IPlatformPublicationReader publicationReader,
    ICalendarEventModifier calendarEvents)
{
    public async Task<UpdateCalendarEventResult> HandleAsync(
        UpdateCalendarEventCommand command,
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

        if (await publicationReader.HasAnyForEventAsync(
                command.CalendarEventId,
                cancellationToken))
        {
            return UpdateCalendarEventResult.HasPlatformPublications;
        }

        ScheduledStartConversion conversion;
        try
        {
            conversion = ScheduledStartConverter.Convert(command.Start);
        }
        catch (InvalidScheduledStartException exception)
        {
            return UpdateCalendarEventResult.Invalid(exception.ValidationError);
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

        bool updated;
        try
        {
            updated = await calendarEvents.UpdateAsync(
                command.CalendarEventId,
                new CalendarEvent(command.Start, text),
                conversion.ScheduledStartUtc,
                cancellationToken);
        }
        catch (DuplicateScheduledStartException exception)
        {
            return UpdateCalendarEventResult.DuplicateScheduledStart(
                exception.ScheduledStartUtc);
        }

        // The row was read but vanished before the write. Treat the
        // stale client as a missing event so the host returns 404.
        return updated
            ? UpdateCalendarEventResult.Updated
            : UpdateCalendarEventResult.NotFound;
    }
}

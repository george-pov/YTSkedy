using YTSkedy.Scheduling.Application.CalendarEvents.Starts;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class UpdateCalendarEventHandler(
    ICalendarEventReader calendarEventReader,
    CalendarEventPublicationLock publicationLock,
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

        if (await publicationLock.IsLockedAsync(
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

        CalendarEventChangeResult changeResult;
        try
        {
            changeResult = await calendarEvents.UpdateAsync(
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

        return changeResult switch
        {
            CalendarEventChangeResult.Applied => UpdateCalendarEventResult.Updated,
            CalendarEventChangeResult.NotFound => UpdateCalendarEventResult.NotFound,
            CalendarEventChangeResult.Conflict => UpdateCalendarEventResult.Conflict,
            _ => throw new InvalidOperationException(
                $"Unknown calendar event change result '{changeResult}'.")
        };
    }
}

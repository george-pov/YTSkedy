using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class CreateCalendarEventHandler(ICalendarEventRepository calendarEvents)
{
    public async Task<CreateCalendarEventResult> HandleAsync(
        CreateCalendarEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = new CalendarEvent(
            command.Start,
            command.Descriptions);

        var eventId = await calendarEvents.CreateAsync(
            calendarEvent,
            cancellationToken);

        return new CreateCalendarEventResult(eventId);
    }
}

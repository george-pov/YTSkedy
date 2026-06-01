using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class CreateEventHandler(ICalendarEventRepository calendarEvents)
{
    public async Task<CreateEventResult> HandleAsync(
        CreateEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = new CalendarEvent(
            command.Start,
            command.Descriptions);

        var eventId = await calendarEvents.CreateAsync(
            calendarEvent,
            cancellationToken);

        return new CreateEventResult(eventId);
    }
}

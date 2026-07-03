using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Application.Settings;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class CreateCalendarEventHandler(
    IEventTextFieldsReader eventTextFields,
    ICalendarEventModifier calendarEvents)
{
    public async Task<CreateCalendarEventResult> HandleAsync(
        CreateCalendarEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        ScheduledStartConversion conversion;
        try
        {
            conversion = ScheduledStartConverter.Convert(command.Start);
        }
        catch (InvalidScheduledStartException exception)
        {
            return CreateCalendarEventResult.Invalid(exception.ValidationError);
        }

        var fields = await eventTextFields.GetAsync(cancellationToken);

        EventTextSnapshot snapshot;
        try
        {
            snapshot = EventTextSnapshot.Create(fields, command.Texts);
        }
        catch (ArgumentException exception)
        {
            return CreateCalendarEventResult.Invalid(exception.Message);
        }

        var calendarEvent = new CalendarEvent(command.Start, snapshot);

        string calendarEventId;
        try
        {
            calendarEventId = await calendarEvents.CreateAsync(
                calendarEvent,
                conversion.ScheduledStartUtc,
                cancellationToken);
        }
        catch (DuplicateScheduledStartException exception)
        {
            return CreateCalendarEventResult.DuplicateScheduledStart(
                exception.ScheduledStartUtc);
        }

        return CreateCalendarEventResult.Created(calendarEventId);
    }
}

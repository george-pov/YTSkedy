namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Updates the localized descriptions of an existing calendar event in place.
/// The scheduled start, identity, and status are left unchanged because the
/// event id is derived from its UTC start instant; changing the start would
/// change identity, which edit does not support. Returns false when no event
/// has the id so the API can map that to a 404.
/// </summary>
public sealed class UpdateCalendarEventHandler(ICalendarEventRepository calendarEvents)
{
    public async Task<bool> HandleAsync(
        UpdateCalendarEventDescriptionsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await calendarEvents.UpdateDescriptionsAsync(
            command.CalendarEventId,
            command.Descriptions,
            cancellationToken);
    }
}

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Updates the localized descriptions of an existing Draft calendar event in
/// place. The scheduled start, identity, and status are left unchanged because
/// the event id is derived from its UTC start instant; changing the start would
/// change identity, which edit does not support. The handler reads the event
/// first and rejects non-Draft rows with
/// <see cref="UpdateCalendarEventResult.NotUpdatable"/> so already-published
/// descriptions cannot drift from the metadata sent to YouTube, even when a
/// stale client still believed the event was editable. A missing event maps to
/// <see cref="UpdateCalendarEventResult.NotFound"/>.
/// </summary>
public sealed class UpdateCalendarEventHandler(
    ICalendarEventReader calendarEventReader,
    ICalendarEventRepository calendarEvents)
{
    public async Task<UpdateCalendarEventResult> HandleAsync(
        UpdateDescriptionsCommand command,
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

        if (!CalendarEventActionPolicy.CanUpdate(calendarEvent.Status))
        {
            return UpdateCalendarEventResult.NotUpdatable;
        }

        var updated = await calendarEvents.UpdateDescriptionsAsync(
            command.CalendarEventId,
            command.Descriptions,
            cancellationToken);

        // The row was read as Draft but vanished before the write. Treat the
        // stale client as a missing event so the host returns 404.
        return updated
            ? UpdateCalendarEventResult.Updated
            : UpdateCalendarEventResult.NotFound;
    }
}

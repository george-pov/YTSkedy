namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// Request body for updating an existing calendar event. Only the localized
/// descriptions can change; the scheduled start is immutable because the event
/// id is derived from it, so the start is intentionally absent here.
/// </summary>
internal sealed record UpdateCalendarEventRequest(
    IReadOnlyList<LocalizedCalendarEventText> Descriptions);

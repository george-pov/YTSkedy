namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// Request body for updating an existing calendar event. The scheduled start
/// and localized text values are replaced together.
/// </summary>
internal sealed record UpdateCalendarEventRequest(
    CalendarEventStart Start,
    IReadOnlyList<EventTextPayload> Texts);

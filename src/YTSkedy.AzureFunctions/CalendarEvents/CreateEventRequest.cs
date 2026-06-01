namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed record CreateEventRequest(
    EventStart Start,
    IReadOnlyList<LocalizedEventText> Descriptions);

public sealed record EventStart(
    DateTime LocalDateTime,
    string TimeZoneId);

public sealed record LocalizedEventText(
    string Language,
    string Title,
    string? Description);

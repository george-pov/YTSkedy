namespace YTSkedy.AzureFunctions.CalendarEvents;

internal sealed record PublishingContentResponse(
    string Kind,
    string Title,
    string? Description);

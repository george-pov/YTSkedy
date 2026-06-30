namespace YTSkedy.AzureFunctions.CalendarEvents;

internal sealed record RenderedPublishingContentResponse(
    string Type,
    string Title,
    string? Description);

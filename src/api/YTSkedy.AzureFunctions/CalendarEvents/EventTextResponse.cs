namespace YTSkedy.AzureFunctions.CalendarEvents;

internal sealed record EventTextResponse(
    string FieldKey,
    string Label,
    string Type,
    int MaxLength,
    string Value);

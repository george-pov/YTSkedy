namespace YTSkedy.AzureFunctions.CalendarEvents;

internal sealed record ThumbnailResponse(
    string FileName,
    string ContentType,
    long SizeBytes,
    int Width,
    int Height,
    DateTimeOffset UpdatedUtc);

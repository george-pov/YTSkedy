namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record Thumbnail(
    string FileName,
    string ContentType,
    long SizeBytes,
    int Width,
    int Height,
    DateTimeOffset UpdatedUtc,
    string BlobName);

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed record UploadThumbnailCommand(
    string CalendarEventId,
    string FileName,
    string ContentType,
    byte[] Content);

namespace YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

public sealed record UploadThumbnailCommand(
    string CalendarEventId,
    string FileName,
    string ContentType,
    byte[] Content);

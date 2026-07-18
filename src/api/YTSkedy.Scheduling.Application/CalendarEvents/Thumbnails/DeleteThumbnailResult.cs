namespace YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

public sealed record DeleteThumbnailResult(DeleteThumbnailStatus Status)
{
    public static DeleteThumbnailResult Deleted { get; } =
        new(DeleteThumbnailStatus.Deleted);

    public static DeleteThumbnailResult EventNotFound { get; } =
        new(DeleteThumbnailStatus.EventNotFound);

    public static DeleteThumbnailResult ThumbnailNotFound { get; } =
        new(DeleteThumbnailStatus.ThumbnailNotFound);

    public static DeleteThumbnailResult HasPlatformPublications { get; } =
        new(DeleteThumbnailStatus.HasPlatformPublications);

    public static DeleteThumbnailResult Conflict { get; } =
        new(DeleteThumbnailStatus.Conflict);
}

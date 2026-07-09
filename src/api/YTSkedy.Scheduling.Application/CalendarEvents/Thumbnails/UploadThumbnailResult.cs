using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

public sealed record UploadThumbnailResult(
    UploadThumbnailStatus Status,
    Thumbnail? Thumbnail = null,
    ThumbnailValidationError? ValidationError = null)
{
    public static UploadThumbnailResult Uploaded(Thumbnail thumbnail) =>
        new(UploadThumbnailStatus.Uploaded, thumbnail);

    public static UploadThumbnailResult Invalid(ThumbnailValidationError error) =>
        new(UploadThumbnailStatus.Invalid, ValidationError: error);

    public static UploadThumbnailResult EventNotFound { get; } =
        new(UploadThumbnailStatus.EventNotFound);

    public static UploadThumbnailResult HasPlatformPublications { get; } =
        new(UploadThumbnailStatus.HasPlatformPublications);
}

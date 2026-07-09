using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

namespace YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;

public sealed record PublicationThumbnail(bool IsConfigured, ThumbnailContent? Content)
{
    public static PublicationThumbnail NotConfigured { get; } = new(false, null);

    public static PublicationThumbnail MissingContent { get; } = new(true, null);

    public static PublicationThumbnail Configured(ThumbnailContent content) =>
        new(true, content);
}

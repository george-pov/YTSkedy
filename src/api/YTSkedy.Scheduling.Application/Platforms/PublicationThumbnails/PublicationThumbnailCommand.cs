using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;

public sealed record PublicationThumbnailCommand(
    string CalendarEventId,
    string PlatformId,
    PlatformView Platform,
    string ExternalResourceId,
    PublicationThumbnail Thumbnail);

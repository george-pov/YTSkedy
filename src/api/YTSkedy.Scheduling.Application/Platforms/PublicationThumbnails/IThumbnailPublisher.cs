using YTSkedy.Scheduling.Application.Platforms.Providers;

namespace YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;

/// <summary>
/// Provider port for applying a calendar-event thumbnail after the primary
/// provider resource has already been created and recorded locally.
/// </summary>
public interface IThumbnailPublisher : IPlatformTypeAdapter
{
    Task PublishAsync(
        ThumbnailPublishRequest request,
        CancellationToken cancellationToken);
}

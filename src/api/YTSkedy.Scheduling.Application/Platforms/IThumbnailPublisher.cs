using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Provider port for applying a calendar-event thumbnail after the primary
/// provider resource has already been created and recorded locally.
/// </summary>
public interface IThumbnailPublisher
{
    PlatformType Type { get; }

    Task PublishAsync(
        ThumbnailPublishRequest request,
        CancellationToken cancellationToken);
}

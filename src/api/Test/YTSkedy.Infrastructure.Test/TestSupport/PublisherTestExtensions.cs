using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.Platforms.Providers;

namespace YTSkedy.Infrastructure.Test.TestSupport;

internal static class PublisherTestExtensions
{
    public static Task<PlatformPublishResult> PublishAsync(
        this YouTubePublisher publisher,
        PlatformPublishRequest request,
        CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            request,
            Mock.Of<IPlatformPublishCheckpoint>(),
            cancellationToken);

    public static Task<PlatformPublishResult> PublishAsync(
        this WordPressPublisher publisher,
        PlatformPublishRequest request,
        CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            request,
            Mock.Of<IPlatformPublishCheckpoint>(),
            cancellationToken);
}

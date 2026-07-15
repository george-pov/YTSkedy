using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Infrastructure.YouTube;

namespace YTSkedy.Infrastructure.Test.TestSupport;

internal sealed class RecordingPublishCheckpoint : IPlatformPublishCheckpoint
{
    public List<string> ExternalResourceIds { get; } = [];

    public List<CancellationToken> CancellationTokens { get; } = [];

    public Exception? Throws { get; init; }

    public Task SaveExternalResourceIdAsync(
        string externalResourceId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExternalResourceIds.Add(externalResourceId);
        CancellationTokens.Add(cancellationToken);
        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.CompletedTask;
    }
}

internal static class PublisherTestExtensions
{
    public static Task<PlatformPublishResult> PublishAsync(
        this YouTubePublisher publisher,
        PlatformPublishRequest request,
        CancellationToken cancellationToken) =>
        publisher.PublishAsync(request, new RecordingPublishCheckpoint(), cancellationToken);

    public static Task<PlatformPublishResult> PublishAsync(
        this WordPressPublisher publisher,
        PlatformPublishRequest request,
        CancellationToken cancellationToken) =>
        publisher.PublishAsync(request, new RecordingPublishCheckpoint(), cancellationToken);
}

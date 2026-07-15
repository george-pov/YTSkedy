using YTSkedy.Scheduling.Application.Platforms.Providers;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public sealed class PublishCheckpoint(
    IPublicationAttemptWriter publicationAttempts,
    string calendarEventId,
    string platformId) : IPlatformPublishCheckpoint
{
    internal string? ExternalResourceId { get; private set; }

    public async Task SaveExternalResourceIdAsync(
        string externalResourceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalResourceId);
        ExternalResourceId = externalResourceId.Trim();

        var result = await publicationAttempts.SaveExternalResourceIdAsync(
            calendarEventId,
            platformId,
            ExternalResourceId,
            cancellationToken);
        if (result != SaveExternalResourceIdResult.Saved)
        {
            throw new InvalidOperationException(
                $"The current publication attempt could not checkpoint its external resource id. " +
                $"Checkpoint result: {result}.");
        }
    }
}

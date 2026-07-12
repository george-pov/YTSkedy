using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

/// <summary>
/// Applies best-effort updates to the calendar-event publication index after the
/// authoritative platform-publication row has changed.
/// </summary>
public sealed class PublicationIndexUpdater(
    IPublicationIndexWriter publicationIndex,
    ILogger<PublicationIndexUpdater> logger)
{
    public Task AddPublishedPlatformAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            "AddPublishedPlatform",
            calendarEventId,
            platformId,
            token => publicationIndex.AddPublishedPlatformAsync(
                calendarEventId,
                platformId,
                token),
            cancellationToken);

    public Task RemovePublishedPlatformAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            "RemovePublishedPlatform",
            calendarEventId,
            platformId,
            token => publicationIndex.RemovePublishedPlatformAsync(
                calendarEventId,
                platformId,
                token),
            cancellationToken);

    private async Task ApplyAsync(
        string operation,
        string calendarEventId,
        string platformId,
        Func<CancellationToken, Task<bool>> update,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await update(cancellationToken))
            {
                LogFailure(operation, calendarEventId, platformId);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailure(exception, operation, calendarEventId, platformId);
        }
    }

    private void LogFailure(
        string operation,
        string calendarEventId,
        string platformId) =>
        logger.LogError(
            "Publication index operation {Operation} failed for calendar event " +
            "{CalendarEventId} and platform {PlatformId}.",
            operation,
            calendarEventId,
            platformId);

    private void LogFailure(
        Exception exception,
        string operation,
        string calendarEventId,
        string platformId) =>
        logger.LogError(
            exception,
            "Publication index operation {Operation} failed for calendar event " +
            "{CalendarEventId} and platform {PlatformId}.",
            operation,
            calendarEventId,
            platformId);
}

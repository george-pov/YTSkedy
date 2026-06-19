using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.YouTube;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Publishes a single draft calendar event as a scheduled YouTube live
/// broadcast. Guards run before any external call: a missing event, an
/// already-published event, a past start instant, or a missing English
/// description short-circuit without contacting YouTube. The event is reserved
/// (Draft to Publishing) before the broadcast call so concurrent publishes
/// cannot create duplicate broadcasts.
///
/// If the broadcast call fails the reservation is released back to Draft. Once
/// the broadcast exists, recording its id is the only thing that keeps it
/// recoverable, so the finalize write is retried across transient storage
/// faults. If it still cannot be recorded, the handler confirms the stored
/// state and, only while the event is still Publishing, deletes the
/// just-created broadcast and releases the reservation so no orphaned, billable
/// broadcast is left untracked. If the stored state shows the write actually
/// landed, the publish is reported as succeeded. If the compensating delete
/// itself fails, or the stored state cannot be confirmed, the row is left
/// Publishing and the broadcast id is logged for manual cleanup rather than
/// silently lost.
/// </summary>
public sealed class PublishYouTubeHandler(
    ICalendarEventReader calendarEventReader,
    ICalendarEventRepository calendarEventRepository,
    IYouTubePublisher youTubePublisher,
    IYouTubeDeleter youTubeDeleter,
    TimeProvider timeProvider,
    ILogger<PublishYouTubeHandler> logger)
{
    // The broadcast already exists by the time the finalize write runs, so a
    // transient storage fault should not be allowed to strand it. Each attempt
    // re-reads a fresh ETag inside the repository, so retrying immediately
    // resolves a stale-ETag conflict or a lost acknowledgement without an
    // artificial delay (kept out for deterministic behavior under test).
    private const int MaxFinalizeAttempts = 3;

    public async Task<PublishYouTubeResult> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var calendarEvent = await calendarEventReader.GetByIdAsync(
            calendarEventId,
            cancellationToken);

        if (calendarEvent is null)
        {
            return PublishYouTubeResult.NotFound();
        }

        if (calendarEvent.Status == CalendarEventStatus.Published)
        {
            return PublishYouTubeResult.AlreadyPublished();
        }

        if (calendarEvent.ScheduledStartUtc <= timeProvider.GetUtcNow())
        {
            return PublishYouTubeResult.StartInPast();
        }

        var englishDescription = calendarEvent.Descriptions.FirstOrDefault(
            description => description.IsEnglish);

        if (englishDescription is null)
        {
            return PublishYouTubeResult.MissingEnglishDescription();
        }

        // Reserve the publish transition before any external call. A concurrent
        // request that already moved the event out of Draft loses the race here,
        // so YouTube is never asked to create a duplicate broadcast. A failed
        // reservation is reported as already published: another request owns the
        // publish and retrying would not help.
        if (!await calendarEventRepository.TryReserveForPublishingAsync(
                calendarEventId,
                cancellationToken))
        {
            return PublishYouTubeResult.AlreadyPublished();
        }

        string broadcastId;

        try
        {
            broadcastId = await youTubePublisher.PublishAsync(
                new YouTubeRequest(
                    englishDescription.Title,
                    englishDescription.Description,
                    calendarEvent.ScheduledStartUtc),
                cancellationToken);
        }
        catch
        {
            // The broadcast was not created. Release the reservation so the
            // event returns to Draft and stays retryable. Best-effort and
            // uncancelled so it still runs when the request itself was canceled,
            // and it must never mask the original broadcast failure.
            await TryReleaseReservationAsync(calendarEventId);

            throw;
        }

        // The broadcast now exists on YouTube. Record its id, undoing the
        // broadcast rather than orphaning it if that cannot be completed.
        return await FinalizePublishAsync(calendarEventId, broadcastId);
    }

    private async Task<PublishYouTubeResult> FinalizePublishAsync(
        string calendarEventId,
        string broadcastId)
    {
        try
        {
            await MarkPublishedWithRetryAsync(calendarEventId, broadcastId);

            return PublishYouTubeResult.Published(broadcastId);
        }
        catch (Exception exception)
        {
            // Retries exhausted. The broadcast exists but its id is not
            // recorded, so without intervention it would be an orphaned,
            // billable resource the API can no longer reach.
            var recovered = await TryConfirmOrCompensateAsync(
                calendarEventId,
                broadcastId,
                exception);

            if (recovered is not null)
            {
                return recovered;
            }

            throw;
        }
    }

    private async Task MarkPublishedWithRetryAsync(
        string calendarEventId,
        string broadcastId)
    {
        for (var attempt = 1; attempt <= MaxFinalizeAttempts; attempt++)
        {
            try
            {
                // Uncancelled: once the broadcast exists, finishing the local
                // record must not be abandoned because the request was canceled.
                await calendarEventRepository.MarkPublishedAsync(
                    calendarEventId,
                    broadcastId,
                    CancellationToken.None);

                return;
            }
            catch (Exception exception)
            {
                // Let the final attempt's failure propagate so the caller can
                // confirm the stored state and compensate.
                if (attempt == MaxFinalizeAttempts)
                {
                    throw;
                }

                logger.LogWarning(
                    exception,
                    "Recording YouTube broadcast {YouTubeBroadcastId} for calendar event "
                        + "{CalendarEventId} failed on attempt {Attempt} of {MaxAttempts}; retrying.",
                    broadcastId,
                    calendarEventId,
                    attempt,
                    MaxFinalizeAttempts);
            }
        }
    }

    /// <summary>
    /// Decides what to do after the finalize write could not complete. Returns a
    /// non-null result when the write had actually landed (the publish
    /// succeeded); returns null when the caller should rethrow the original
    /// failure, after compensating where it is safe to do so.
    /// </summary>
    private async Task<PublishYouTubeResult?> TryConfirmOrCompensateAsync(
        string calendarEventId,
        string broadcastId,
        Exception markException)
    {
        CalendarEventView? current;

        try
        {
            current = await calendarEventReader.GetByIdAsync(
                calendarEventId,
                CancellationToken.None);
        }
        catch (Exception readException)
        {
            // State is unknown. Deleting could destroy a broadcast whose publish
            // actually succeeded, so keep it and surface the id for manual
            // cleanup instead.
            logger.LogError(
                readException,
                "Could not confirm calendar event {CalendarEventId} state after failing to "
                    + "record YouTube broadcast {YouTubeBroadcastId}. The broadcast may be "
                    + "orphaned and needs manual cleanup.",
                calendarEventId,
                broadcastId);

            return null;
        }

        if (current is { Status: CalendarEventStatus.Published })
        {
            // A lost acknowledgement: the finalize write had applied even though
            // the call threw. The publish succeeded.
            logger.LogInformation(
                "Recording YouTube broadcast {YouTubeBroadcastId} for calendar event "
                    + "{CalendarEventId} was confirmed after a transient finalize failure.",
                broadcastId,
                calendarEventId);

            return PublishYouTubeResult.Published(broadcastId);
        }

        if (current is not { Status: CalendarEventStatus.Publishing })
        {
            // The row is gone or in an unexpected state. Do not delete a
            // broadcast that may belong to a different outcome; surface the id.
            logger.LogError(
                markException,
                "Calendar event {CalendarEventId} was not in a recoverable state after failing "
                    + "to record YouTube broadcast {YouTubeBroadcastId}. The broadcast may be "
                    + "orphaned and needs manual cleanup.",
                calendarEventId,
                broadcastId);

            return null;
        }

        // Confirmed still Publishing, so the broadcast is genuinely orphaned.
        // Log the id first, then delete the broadcast and release the
        // reservation back to Draft so the event stays retryable.
        logger.LogError(
            markException,
            "Failed to record YouTube broadcast {YouTubeBroadcastId} for calendar event "
                + "{CalendarEventId}; deleting the orphaned broadcast and releasing the reservation.",
            broadcastId,
            calendarEventId);

        try
        {
            // A not-found broadcast is success-equivalent: the intended end
            // state (no untracked broadcast) already holds.
            await youTubeDeleter.DeleteAsync(broadcastId, CancellationToken.None);
        }
        catch (YouTubeDeleteException deleteException)
        {
            // The broadcast may still exist. Do not release to Draft, because a
            // retry could then create a duplicate. Keep it Publishing and
            // surface the id for manual cleanup.
            logger.LogError(
                deleteException,
                "Failed to delete orphaned YouTube broadcast {YouTubeBroadcastId} for calendar "
                    + "event {CalendarEventId}. The broadcast needs manual cleanup.",
                broadcastId,
                calendarEventId);

            return null;
        }

        await TryReleaseReservationAsync(calendarEventId);

        return null;
    }

    private async Task TryReleaseReservationAsync(string calendarEventId)
    {
        try
        {
            await calendarEventRepository.ReleaseReservationAsync(
                calendarEventId,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            // Compensation must never mask the original failure being unwound.
            logger.LogError(
                exception,
                "Failed to release the publish reservation for calendar event {CalendarEventId}.",
                calendarEventId);
        }
    }
}

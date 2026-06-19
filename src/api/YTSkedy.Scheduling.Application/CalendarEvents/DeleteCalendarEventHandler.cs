using YTSkedy.Scheduling.Application.YouTube;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Deletes a single calendar event. A missing event short-circuits to
/// <see cref="DeleteCalendarEventResult.NotFound"/>. A Draft event keeps the
/// existing local-cleanup path: it is handed to the repository, whose
/// ETag-conditional delete owns the final race and can still report NotFound if
/// the row vanished or NotDeletable if a concurrent write advanced it out of
/// Draft. A future Published event with a recorded broadcast id is deleted from
/// YouTube first (treating an already-gone broadcast as success-equivalent) and
/// then removed locally by id without rechecking status. A future Published
/// event with no recorded broadcast id is kept and reported as
/// <see cref="DeleteCalendarEventResult.MissingYouTubeBroadcastId"/>, a
/// Publishing or past Published event is
/// <see cref="DeleteCalendarEventResult.NotDeletable"/>, and a YouTube delete
/// failure keeps the local row and reports
/// <see cref="DeleteCalendarEventResult.YouTubeDeleteFailed"/>. Eligibility is
/// shared with the read models through <see cref="CalendarEventActionPolicy"/>;
/// the handler stays authoritative for races and stale clients.
/// </summary>
public sealed class DeleteCalendarEventHandler(
    ICalendarEventReader calendarEventReader,
    ICalendarEventRepository calendarEventRepository,
    IYouTubeDeleter youTubeDeleter,
    TimeProvider timeProvider)
{
    public async Task<DeleteCalendarEventResult> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        // One application read decides eligibility and supplies the broadcast id.
        // No fresh snapshot is taken before the YouTube call, and the local row
        // is deleted by id afterwards without re-reading status.
        var calendarEvent = await calendarEventReader.GetByIdAsync(
            calendarEventId,
            cancellationToken);

        if (calendarEvent is null)
        {
            return DeleteCalendarEventResult.NotFound;
        }

        if (calendarEvent.Status == CalendarEventStatus.Draft)
        {
            return await DeleteDraftAsync(calendarEventId, cancellationToken);
        }

        var nowUtc = timeProvider.GetUtcNow();

        // A future Published row with no recorded broadcast id is a distinct
        // not-deletable case: the local row is kept and the host explains why.
        // CanDelete also rejects it, but a single bool cannot carry the reason.
        if (calendarEvent.Status == CalendarEventStatus.Published &&
            calendarEvent.ScheduledStartUtc > nowUtc &&
            string.IsNullOrWhiteSpace(calendarEvent.YouTubeBroadcastId))
        {
            return DeleteCalendarEventResult.MissingYouTubeBroadcastId;
        }

        if (!CalendarEventActionPolicy.CanDelete(
                calendarEvent.Status,
                calendarEvent.ScheduledStartUtc,
                calendarEvent.YouTubeBroadcastId,
                nowUtc))
        {
            return DeleteCalendarEventResult.NotDeletable;
        }

        // Only a future Published event with a recorded broadcast id reaches
        // here, so the broadcast id is guaranteed non-blank by the policy.
        try
        {
            // Deleting the broadcast and finding it already gone are both
            // success-equivalent: the intended external end state is reached.
            await youTubeDeleter.DeleteAsync(
                calendarEvent.YouTubeBroadcastId!,
                cancellationToken);
        }
        catch (YouTubeDeleteException)
        {
            // The broadcast may still exist. Keep the local row so the operator
            // can retry; the host maps this to 502 Bad Gateway.
            return DeleteCalendarEventResult.YouTubeDeleteFailed;
        }

        // Delete the local row by id without rechecking status. A row that
        // disappeared after successful YouTube cleanup is success-equivalent
        // because both the external and local resources are now gone.
        await calendarEventRepository.DeleteAsync(calendarEventId, cancellationToken);

        return DeleteCalendarEventResult.Deleted;
    }

    private async Task<DeleteCalendarEventResult> DeleteDraftAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        var result = await calendarEventRepository.DeleteDraftAsync(
            calendarEventId,
            cancellationToken);

        return result switch
        {
            DeleteDraftCalendarEventResult.Deleted => DeleteCalendarEventResult.Deleted,
            DeleteDraftCalendarEventResult.NotFound => DeleteCalendarEventResult.NotFound,
            _ => DeleteCalendarEventResult.NotDeletable
        };
    }
}

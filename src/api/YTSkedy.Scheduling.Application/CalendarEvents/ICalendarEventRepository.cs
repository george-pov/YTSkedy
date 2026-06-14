using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface ICalendarEventRepository
{
    Task<string> CreateAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically moves a Draft event to Publishing so only one publish can
    /// proceed to YouTube. Returns false when the event is not currently Draft
    /// (missing, already publishing, already published, or a concurrent
    /// reservation won the race).
    /// </summary>
    Task<bool> TryReserveForPublishingAsync(
        string calendarEventId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks a reserved event Published and records its YouTube broadcast id.
    /// </summary>
    Task MarkPublishedAsync(
        string calendarEventId,
        string youTubeBroadcastId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a reserved event to Draft after a failed broadcast attempt so it
    /// stays retryable.
    /// </summary>
    Task ReleaseReservationAsync(
        string calendarEventId,
        CancellationToken cancellationToken);
}

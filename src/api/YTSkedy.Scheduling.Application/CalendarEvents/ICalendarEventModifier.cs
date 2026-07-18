using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface ICalendarEventModifier
{
    Task<string> CreateAsync(
        CalendarEvent calendarEvent,
        DateTimeOffset scheduledStartUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the stored scheduled start and event text snapshot of an
    /// existing event in place. A conflict means the row changed after the
    /// repository read and the caller must rerun use-case guards.
    /// </summary>
    Task<CalendarEventChangeResult> UpdateAsync(
        string calendarEventId,
        CalendarEvent calendarEvent,
        DateTimeOffset scheduledStartUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a calendar event row by id. The delete is unconditional and
    /// idempotent: a missing row is a no-op. Storage identity and ETags stay
    /// inside infrastructure.
    /// </summary>
    Task DeleteAsync(
        string calendarEventId,
        CancellationToken cancellationToken);
}

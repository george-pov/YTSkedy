using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface ICalendarEventModifier
{
    Task<string> CreateAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the stored event text snapshot of an existing event in place,
    /// leaving its scheduled start and identity unchanged. Returns false when
    /// no event has the id.
    /// </summary>
    Task<bool> UpdateTextAsync(
        string calendarEventId,
        EventTextSnapshot text,
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

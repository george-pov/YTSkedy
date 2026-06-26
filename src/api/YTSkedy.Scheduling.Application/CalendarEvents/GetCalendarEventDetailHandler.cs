using YTSkedy.Scheduling.Application.Platforms;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Loads one calendar event with its per-platform publication state for the
/// event detail view. The calendar event is read once: a missing event maps to
/// null so the boundary returns <c>404 Not Found</c>, and a found event is
/// paired with the event-platform projection over active platforms and stored
/// publication rows. The calendar event stays provider-neutral; publish state is
/// composed here rather than stored on the event.
/// </summary>
public sealed class GetCalendarEventDetailHandler(
    ICalendarEventReader calendarEvents,
    IPlatformReader platforms,
    IPlatformPublicationReader publications,
    TimeProvider timeProvider)
{
    public async Task<CalendarEventDetailView?> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var calendarEvent = await calendarEvents.GetByIdAsync(calendarEventId, cancellationToken);

        if (calendarEvent is null)
        {
            return null;
        }

        var activePlatforms = await platforms.ListAsync(null, cancellationToken);
        var publicationRows = await publications.ListByEventAsync(calendarEventId, cancellationToken);

        return new CalendarEventDetailView(
            calendarEvent,
            EventPlatformProjection.Project(
                calendarEvent,
                activePlatforms,
                publicationRows,
                timeProvider.GetUtcNow()));
    }
}

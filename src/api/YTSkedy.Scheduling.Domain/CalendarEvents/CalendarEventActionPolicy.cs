namespace YTSkedy.Scheduling.Domain.CalendarEvents;

/// <summary>
/// Single source of truth for calendar event action eligibility. The publish,
/// update, and delete use cases share these rules so the read models that drive
/// the UI (CanPublish, CanUpdate, CanDelete) can never disagree with what the
/// handlers actually allow. The rules are pure: callers supply the current
/// instant, so eligibility never depends on hidden local-machine time.
/// </summary>
public static class CalendarEventActionPolicy
{
    /// <summary>
    /// A calendar event is publishable when it is still a Draft, its scheduled
    /// start is in the future, and it has an English description to publish.
    /// This mirrors the granular guards in the publish handler.
    /// </summary>
    public static bool CanPublish(
        CalendarEventStatus status,
        DateTimeOffset scheduledStartUtc,
        IReadOnlyList<LocalizedDescription> descriptions,
        DateTimeOffset nowUtc) =>
        status == CalendarEventStatus.Draft &&
        scheduledStartUtc > nowUtc &&
        descriptions.Any(description => description.IsEnglish);

    /// <summary>
    /// Only Draft events can be updated. Once an event is Publishing or
    /// Published its local descriptions are frozen so they cannot drift from the
    /// metadata already sent to YouTube.
    /// </summary>
    public static bool CanUpdate(CalendarEventStatus status) =>
        status == CalendarEventStatus.Draft;

    /// <summary>
    /// A Draft event is always deletable as local cleanup, including past Draft
    /// rows. A Published event is deletable only when its scheduled start is in
    /// the future and a YouTube broadcast id is recorded, so the external
    /// broadcast can be removed before the local row. Publishing events and past
    /// Published events are never deletable.
    /// </summary>
    public static bool CanDelete(
        CalendarEventStatus status,
        DateTimeOffset scheduledStartUtc,
        string? youTubeBroadcastId,
        DateTimeOffset nowUtc) =>
        status switch
        {
            CalendarEventStatus.Draft => true,
            CalendarEventStatus.Published =>
                scheduledStartUtc > nowUtc &&
                !string.IsNullOrWhiteSpace(youTubeBroadcastId),
            _ => false
        };
}

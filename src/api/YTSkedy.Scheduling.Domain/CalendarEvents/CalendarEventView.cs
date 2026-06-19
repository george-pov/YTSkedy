namespace YTSkedy.Scheduling.Domain.CalendarEvents;

/// <summary>
/// Calendar event read model behind the list, detail, publish, and delete use
/// cases. Carries the stored YouTube broadcast id (null until published) and
/// exposes the action eligibility the UI consumes. The flags delegate to
/// <see cref="CalendarEventActionPolicy"/> so they always match the publish,
/// update, and delete handlers; callers pass the current instant so eligibility
/// never depends on hidden local-machine time.
/// </summary>
public sealed record CalendarEventView(
    string CalendarEventId,
    ScheduledStart Start,
    DateTimeOffset ScheduledStartUtc,
    IReadOnlyList<LocalizedDescription> Descriptions,
    CalendarEventStatus Status,
    string? YouTubeBroadcastId = null)
{
    public bool CanPublish(DateTimeOffset nowUtc) =>
        CalendarEventActionPolicy.CanPublish(Status, ScheduledStartUtc, Descriptions, nowUtc);

    public bool CanUpdate() =>
        CalendarEventActionPolicy.CanUpdate(Status);

    public bool CanDelete(DateTimeOffset nowUtc) =>
        CalendarEventActionPolicy.CanDelete(Status, ScheduledStartUtc, YouTubeBroadcastId, nowUtc);
}

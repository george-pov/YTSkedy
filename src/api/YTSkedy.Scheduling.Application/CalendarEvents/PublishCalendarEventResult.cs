namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Outcome of a publish attempt. <see cref="YouTubeBroadcastId"/> is set only
/// when <see cref="Outcome"/> is <see cref="PublishCalendarEventOutcome.Published"/>.
/// </summary>
public sealed record PublishCalendarEventResult(
    PublishCalendarEventOutcome Outcome,
    string? YouTubeBroadcastId)
{
    public static PublishCalendarEventResult Published(string youTubeBroadcastId) =>
        new(PublishCalendarEventOutcome.Published, youTubeBroadcastId);

    public static PublishCalendarEventResult NotFound() =>
        new(PublishCalendarEventOutcome.NotFound, null);

    public static PublishCalendarEventResult AlreadyPublished() =>
        new(PublishCalendarEventOutcome.AlreadyPublished, null);

    public static PublishCalendarEventResult StartInPast() =>
        new(PublishCalendarEventOutcome.StartInPast, null);

    public static PublishCalendarEventResult MissingEnglishDescription() =>
        new(PublishCalendarEventOutcome.MissingEnglishDescription, null);
}

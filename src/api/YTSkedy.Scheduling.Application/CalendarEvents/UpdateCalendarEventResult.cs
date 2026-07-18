namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Outcome of an update-calendar-event use case. <see cref="Updated"/> and
/// <see cref="NotFound"/> are stateless singletons; <see cref="Invalid"/>
/// carries a validation message for the API to surface as a 400.
/// </summary>
public sealed record UpdateCalendarEventResult
{
    private UpdateCalendarEventResult(
        UpdateCalendarEventStatus status,
        string? validationError,
        DateTimeOffset? scheduledStartUtc)
    {
        Status = status;
        ValidationError = validationError;
        ScheduledStartUtc = scheduledStartUtc;
    }

    public UpdateCalendarEventStatus Status { get; }

    public string? ValidationError { get; }

    public DateTimeOffset? ScheduledStartUtc { get; }

    public static UpdateCalendarEventResult Updated { get; } =
        new(UpdateCalendarEventStatus.Updated, null, null);

    public static UpdateCalendarEventResult NotFound { get; } =
        new(UpdateCalendarEventStatus.NotFound, null, null);

    public static UpdateCalendarEventResult HasPlatformPublications { get; } =
        new(UpdateCalendarEventStatus.HasPlatformPublications, null, null);

    public static UpdateCalendarEventResult Conflict { get; } =
        new(UpdateCalendarEventStatus.Conflict, null, null);

    public static UpdateCalendarEventResult Invalid(string validationError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validationError);

        return new(UpdateCalendarEventStatus.Invalid, validationError, null);
    }

    public static UpdateCalendarEventResult DuplicateScheduledStart(
        DateTimeOffset scheduledStartUtc) =>
        new(
            UpdateCalendarEventStatus.DuplicateScheduledStart,
            null,
            scheduledStartUtc);
}

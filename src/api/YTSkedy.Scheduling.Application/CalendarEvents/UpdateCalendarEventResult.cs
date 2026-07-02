namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Outcome of an update-calendar-event-text use case. <see cref="Updated"/> and
/// <see cref="NotFound"/> are stateless singletons; <see cref="Invalid"/> carries a
/// validation message for the API to surface as a 400. The domain-invariant check
/// (new text values against the stored snapshot) is state-dependent, so it is
/// reported as a result rather than thrown across the boundary.
/// </summary>
public sealed record UpdateCalendarEventResult
{
    private UpdateCalendarEventResult(
        UpdateCalendarEventStatus status,
        string? validationError)
    {
        Status = status;
        ValidationError = validationError;
    }

    public UpdateCalendarEventStatus Status { get; }

    public string? ValidationError { get; }

    public static UpdateCalendarEventResult Updated { get; } =
        new(UpdateCalendarEventStatus.Updated, null);

    public static UpdateCalendarEventResult NotFound { get; } =
        new(UpdateCalendarEventStatus.NotFound, null);

    public static UpdateCalendarEventResult Invalid(string validationError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validationError);

        return new(UpdateCalendarEventStatus.Invalid, validationError);
    }
}

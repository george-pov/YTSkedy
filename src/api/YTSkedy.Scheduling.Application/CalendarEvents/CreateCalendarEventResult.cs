namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Outcome of a create-calendar-event use case. <see cref="CreateCalendarEventStatus.Created"/>
/// carries the new event id; <see cref="CreateCalendarEventStatus.Invalid"/> carries a
/// validation message for the API to surface as a 400. The domain-invariant check
/// (event text values against the configured fields) is state-dependent, so it is
/// reported as a result rather than thrown across the boundary.
/// </summary>
public sealed record CreateCalendarEventResult
{
    private CreateCalendarEventResult(
        CreateCalendarEventStatus status,
        string? calendarEventId,
        string? validationError)
    {
        Status = status;
        CalendarEventId = calendarEventId;
        ValidationError = validationError;
    }

    public CreateCalendarEventStatus Status { get; }

    public string? CalendarEventId { get; }

    public string? ValidationError { get; }

    public static CreateCalendarEventResult Created(string calendarEventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        return new(CreateCalendarEventStatus.Created, calendarEventId, null);
    }

    public static CreateCalendarEventResult Invalid(string validationError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validationError);

        return new(CreateCalendarEventStatus.Invalid, null, validationError);
    }
}

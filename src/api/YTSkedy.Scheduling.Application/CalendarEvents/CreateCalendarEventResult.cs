namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Outcome of a create-calendar-event use case. <see cref="CreateCalendarEventStatus.Created"/>
/// carries the new event id; <see cref="CreateCalendarEventStatus.Invalid"/>
/// carries a validation message for the API to surface as a 400; and
/// <see cref="CreateCalendarEventStatus.DuplicateScheduledStart"/> carries the
/// duplicate instant for a 409 response.
/// </summary>
public sealed record CreateCalendarEventResult
{
    private CreateCalendarEventResult(
        CreateCalendarEventStatus status,
        string? calendarEventId,
        string? validationError,
        DateTimeOffset? scheduledStartUtc)
    {
        Status = status;
        CalendarEventId = calendarEventId;
        ValidationError = validationError;
        ScheduledStartUtc = scheduledStartUtc;
    }

    public CreateCalendarEventStatus Status { get; }

    public string? CalendarEventId { get; }

    public string? ValidationError { get; }

    public DateTimeOffset? ScheduledStartUtc { get; }

    public static CreateCalendarEventResult Created(string calendarEventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        return new(CreateCalendarEventStatus.Created, calendarEventId, null, null);
    }

    public static CreateCalendarEventResult Invalid(string validationError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validationError);

        return new(CreateCalendarEventStatus.Invalid, null, validationError, null);
    }

    public static CreateCalendarEventResult DuplicateScheduledStart(
        DateTimeOffset scheduledStartUtc) =>
        new(
            CreateCalendarEventStatus.DuplicateScheduledStart,
            null,
            null,
            scheduledStartUtc);
}

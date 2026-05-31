namespace YTSkedy.ConsoleApp;

internal sealed class CalendarEventParseResult
{
    private CalendarEventParseResult(
        IReadOnlyList<CalendarEventInput> events,
        IReadOnlyList<CalendarEventParseError> errors)
    {
        Events = events;
        Errors = errors;
    }

    public bool Succeeded => Errors.Count == 0;

    public IReadOnlyList<CalendarEventInput> Events { get; }

    public IReadOnlyList<CalendarEventParseError> Errors { get; }

    public static CalendarEventParseResult Success(IReadOnlyList<CalendarEventInput> events)
    {
        return new CalendarEventParseResult(events, []);
    }

    public static CalendarEventParseResult Failure(params CalendarEventParseError[] errors)
    {
        return new CalendarEventParseResult([], errors);
    }
}

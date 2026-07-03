namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class InvalidScheduledStartException : Exception
{
    public InvalidScheduledStartException(string validationError)
        : base(validationError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validationError);

        ValidationError = validationError;
    }

    public string ValidationError { get; }
}

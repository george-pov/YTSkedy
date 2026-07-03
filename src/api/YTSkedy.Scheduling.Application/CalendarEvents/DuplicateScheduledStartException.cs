namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class DuplicateScheduledStartException : Exception
{
    public DuplicateScheduledStartException(
        DateTimeOffset scheduledStartUtc,
        Exception? innerException = null)
        : base(
            $"Calendar event scheduled for '{scheduledStartUtc:o}' already exists.",
            innerException)
    {
        ScheduledStartUtc = scheduledStartUtc;
    }

    public DateTimeOffset ScheduledStartUtc { get; }
}

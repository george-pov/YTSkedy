namespace YTSkedy.Scheduling.TestSupport;

public static class SchedulingSampleTimes
{
    public static readonly DateTimeOffset Now =
        new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset FutureStart =
        new(2026, 6, 25, 17, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset PublishedUtc = Now;

    public static readonly DateTimeOffset UpdatedUtc = Now;
}

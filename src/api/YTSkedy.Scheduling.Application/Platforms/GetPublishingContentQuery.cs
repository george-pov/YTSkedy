namespace YTSkedy.Scheduling.Application.Platforms;

public sealed record GetPublishingContentQuery(
    string CalendarEventId,
    string PlatformId);

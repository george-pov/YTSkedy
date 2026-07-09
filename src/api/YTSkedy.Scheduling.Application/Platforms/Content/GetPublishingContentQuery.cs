namespace YTSkedy.Scheduling.Application.Platforms.Content;

public sealed record GetPublishingContentQuery(
    string CalendarEventId,
    string PlatformId);

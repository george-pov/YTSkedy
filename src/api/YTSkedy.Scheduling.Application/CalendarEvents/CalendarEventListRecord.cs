using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed record CalendarEventListRecord(
    CalendarEventView Event,
    IReadOnlySet<string> PublishedPlatformIds);

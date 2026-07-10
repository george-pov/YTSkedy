using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed record CalendarEventListItem(
    CalendarEventView Event,
    PublishingStatus PublicationStatus);

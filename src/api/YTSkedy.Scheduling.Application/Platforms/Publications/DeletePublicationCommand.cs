namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public sealed record DeletePublicationCommand(
    string CalendarEventId,
    string PlatformId);

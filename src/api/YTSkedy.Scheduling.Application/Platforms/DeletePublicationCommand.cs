namespace YTSkedy.Scheduling.Application.Platforms;

public sealed record DeletePublicationCommand(
    string CalendarEventId,
    string PlatformId);

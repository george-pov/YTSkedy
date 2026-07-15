namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public sealed record RecoverPublicationCommand(
    string CalendarEventId,
    string PlatformId);

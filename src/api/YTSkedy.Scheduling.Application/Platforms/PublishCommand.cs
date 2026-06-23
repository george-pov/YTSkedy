namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Request to publish one calendar event to one selected platform. Both ids come
/// from the publish route; the request body is empty in this iteration.
/// </summary>
public sealed record PublishCommand(
    string CalendarEventId,
    string PlatformId);

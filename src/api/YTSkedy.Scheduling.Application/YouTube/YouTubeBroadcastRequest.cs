namespace YTSkedy.Scheduling.Application.YouTube;

/// <summary>
/// Input for creating a scheduled YouTube live broadcast. Title and start
/// instant are required; description is optional. Shared static metadata
/// (privacy, made-for-kids) is supplied by the publisher implementation.
/// </summary>
public sealed record YouTubeBroadcastRequest(
    string Title,
    string? Description,
    DateTimeOffset ScheduledStartUtc);

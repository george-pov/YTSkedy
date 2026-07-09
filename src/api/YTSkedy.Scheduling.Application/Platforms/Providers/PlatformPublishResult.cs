namespace YTSkedy.Scheduling.Application.Platforms.Providers;

/// <summary>
/// Result of a successful provider publish. Carries the provider-neutral
/// <see cref="ExternalResourceId"/> (for YouTube, the created live broadcast id)
/// that the publish use case records on the publication row.
/// </summary>
public sealed record PlatformPublishResult(
    string ExternalResourceId);

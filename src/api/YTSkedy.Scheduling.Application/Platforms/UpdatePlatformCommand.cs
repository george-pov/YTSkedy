using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Update-input for an existing platform. The type is immutable, so only the
/// name, optional reference key, and publish settings can change.
/// </summary>
public sealed record UpdatePlatformCommand(
    string PlatformId,
    string Name,
    string? ReferenceKey,
    PublishSettings PublishSettings);

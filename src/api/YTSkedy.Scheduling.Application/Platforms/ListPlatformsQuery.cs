using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Platform list query. <see cref="Type"/> is optional; when set the read is
/// scoped to that provider type, otherwise platforms of every type are
/// candidates.
/// </summary>
public sealed record ListPlatformsQuery(
    PlatformType? Type);

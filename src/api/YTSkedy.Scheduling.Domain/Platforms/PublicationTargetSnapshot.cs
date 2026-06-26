namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Secret-free provider target data copied to a platform-publication row when a
/// publish attempt starts. It lets delete cleanup prove the active platform still
/// points at the provider target that created the external resource.
/// </summary>
public sealed record PublicationTargetSnapshot(
    PlatformType PlatformType,
    string? WordPressSiteUrl,
    string? YouTubeClientId);

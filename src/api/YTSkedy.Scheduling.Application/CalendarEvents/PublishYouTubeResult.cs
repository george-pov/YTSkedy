namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Outcome of a publish attempt. <see cref="YouTubeBroadcastId"/> is set only
/// when <see cref="Status"/> is <see cref="PublishYouTubeStatus.Published"/>.
/// </summary>
public sealed record PublishYouTubeResult(
    PublishYouTubeStatus Status,
    string? YouTubeBroadcastId)
{
    public static PublishYouTubeResult Published(string youTubeBroadcastId) =>
        new(PublishYouTubeStatus.Published, youTubeBroadcastId);

    public static PublishYouTubeResult NotFound() =>
        new(PublishYouTubeStatus.NotFound, null);

    public static PublishYouTubeResult AlreadyPublished() =>
        new(PublishYouTubeStatus.AlreadyPublished, null);

    public static PublishYouTubeResult StartInPast() =>
        new(PublishYouTubeStatus.StartInPast, null);

    public static PublishYouTubeResult MissingEnglishDescription() =>
        new(PublishYouTubeStatus.MissingEnglishDescription, null);
}

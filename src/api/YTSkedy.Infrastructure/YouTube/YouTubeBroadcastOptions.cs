namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Shared static broadcast metadata applied to every published stream, except
/// title, description, and scheduled start time which come from the calendar
/// event. Bound from the <c>YouTubeBroadcast</c> configuration section.
/// </summary>
public sealed class YouTubeBroadcastOptions
{
    public const string SectionName = "YouTubeBroadcast";

    /// <summary>
    /// YouTube privacy status applied to created broadcasts. Defaults to
    /// <c>private</c> for the proof of concept.
    /// </summary>
    public string PrivacyStatus { get; init; } = "private";

    /// <summary>
    /// Self-declared made-for-kids flag required by the YouTube API. Defaults
    /// to <c>false</c>.
    /// </summary>
    public bool SelfDeclaredMadeForKids { get; init; }
}

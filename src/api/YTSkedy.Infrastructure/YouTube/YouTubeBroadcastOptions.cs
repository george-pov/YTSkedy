using System.ComponentModel.DataAnnotations;

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
    /// YouTube privacy status applied to created broadcasts. Must be one of the
    /// lowercase values the YouTube API accepts: <c>private</c>, <c>public</c>,
    /// or <c>unlisted</c>. A wrong value fails validation on host start instead
    /// of surfacing later as a YouTube API error. Defaults to <c>private</c>.
    /// </summary>
    [AllowedValues(
        "private",
        "public",
        "unlisted",
        ErrorMessage =
            "YouTubeBroadcast:PrivacyStatus must be one of: private, public, unlisted.")]
    public string PrivacyStatus { get; init; } = "private";

    /// <summary>
    /// Self-declared made-for-kids flag required by the YouTube API. Defaults
    /// to <c>false</c>.
    /// </summary>
    public bool SelfDeclaredMadeForKids { get; init; }
}

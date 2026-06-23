namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Named YouTube channel credentials bound from the <c>YouTubeChannels</c>
/// configuration section, keyed by the platform's non-secret credentials
/// reference name. Each entry holds the static Google OAuth secrets for one
/// channel. The section is operator-configured and kept out of source control.
/// </summary>
public sealed class YouTubeOptions : Dictionary<string, YouTubeCredentials>
{
    public const string SectionName = "YouTubeChannels";
}

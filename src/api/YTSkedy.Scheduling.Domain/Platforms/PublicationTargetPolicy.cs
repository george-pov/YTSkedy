namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Compares an active platform with the secret-free target snapshot stored on a
/// platform publication. A mismatch means provider cleanup must not run because
/// the current platform may point at a different provider target.
/// </summary>
public static class PublicationTargetPolicy
{
    public static bool Matches(PlatformView platform, PublicationTargetSnapshot? snapshot)
    {
        ArgumentNullException.ThrowIfNull(platform);

        if (snapshot is null || platform.Type != snapshot.PlatformType)
        {
            return false;
        }

        return platform.Type switch
        {
            PlatformType.YouTube => MatchesYouTube(platform.PublishSettings, snapshot),
            PlatformType.WordPress => MatchesWordPress(platform.PublishSettings, snapshot),
            _ => false
        };
    }

    private static bool MatchesYouTube(
        PublishSettings settings,
        PublicationTargetSnapshot snapshot) =>
        settings is YouTubeSettings youTubeSettings &&
        !string.IsNullOrWhiteSpace(snapshot.YouTubeClientId) &&
        string.Equals(
            youTubeSettings.Credentials.ClientId,
            snapshot.YouTubeClientId,
            StringComparison.Ordinal);

    private static bool MatchesWordPress(
        PublishSettings settings,
        PublicationTargetSnapshot snapshot) =>
        settings is WordPressSettings wordPressSettings &&
        !string.IsNullOrWhiteSpace(snapshot.WordPressSiteUrl) &&
        string.Equals(
            NormalizeSiteUrl(wordPressSettings.SiteUrl),
            NormalizeSiteUrl(snapshot.WordPressSiteUrl),
            StringComparison.OrdinalIgnoreCase);

    private static string NormalizeSiteUrl(string siteUrl)
    {
        if (!Uri.TryCreate(siteUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return siteUrl.Trim();
        }

        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.TrimEnd('/')
        };

        return builder.Uri.GetLeftPart(UriPartial.Path);
    }
}

using System.Globalization;

namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Validates WordPress publication navigation URLs and builds a deterministic
/// plain-permalink fallback from the immutable publication target snapshot.
/// </summary>
public static class WordPressPublicationUrlPolicy
{
    public const int MaxUrlLength = 2048;

    public static string? NormalizeCanonical(
        string? externalResourceUrl,
        string? wordpressSiteUrl)
    {
        if (!TryCreateSafeUri(externalResourceUrl, out var externalResourceUri) ||
            !TryCreateSafeUri(wordpressSiteUrl, out var wordpressSiteUri) ||
            !SameOrigin(externalResourceUri, wordpressSiteUri))
        {
            return null;
        }

        return externalResourceUri.AbsoluteUri;
    }

    public static string? BuildFallback(
        string? wordpressSiteUrl,
        string? externalResourceId)
    {
        if (!TryCreateSafeUri(wordpressSiteUrl, out var wordpressSiteUri) ||
            string.IsNullOrEmpty(externalResourceId) ||
            externalResourceId.Any(character => character is < '0' or > '9') ||
            !long.TryParse(
                externalResourceId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var postId) ||
            postId <= 0)
        {
            return null;
        }

        var builder = new UriBuilder(wordpressSiteUri)
        {
            Query = $"p={postId.ToString(CultureInfo.InvariantCulture)}",
            Fragment = string.Empty
        };
        var fallback = builder.Uri.AbsoluteUri;

        return fallback.Length <= MaxUrlLength ? fallback : null;
    }

    public static string? Resolve(
        string? externalResourceUrl,
        string? wordpressSiteUrl,
        string? externalResourceId) =>
        NormalizeCanonical(externalResourceUrl, wordpressSiteUrl) ??
        BuildFallback(wordpressSiteUrl, externalResourceId);

    private static bool TryCreateSafeUri(string? value, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (normalized.Length > MaxUrlLength ||
            !WordPressSettings.IsValidSiteUrl(normalized) ||
            !Uri.TryCreate(normalized, UriKind.Absolute, out var parsedUri))
        {
            return false;
        }

        uri = parsedUri;
        return true;
    }

    private static bool SameOrigin(Uri first, Uri second) =>
        string.Equals(first.Scheme, second.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(first.DnsSafeHost, second.DnsSafeHost, StringComparison.OrdinalIgnoreCase) &&
        first.Port == second.Port;
}

using System.Globalization;

namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Builds and validates deterministic WordPress post editor URLs from the
/// immutable publication target snapshot and provider post id.
/// </summary>
public static class WordPressPublicationUrlPolicy
{
    public const int MaxUrlLength = 2048;

    public static string? NormalizeAdminEditUrl(
        string? externalResourceUrl,
        string? wordpressSiteUrl,
        string? externalResourceId)
    {
        var expectedUrl = BuildAdminEditUrl(wordpressSiteUrl, externalResourceId);
        if (expectedUrl is null ||
            !TryCreateSafeUri(externalResourceUrl, out var externalResourceUri) ||
            !string.Equals(
                externalResourceUri.AbsoluteUri,
                expectedUrl,
                StringComparison.Ordinal))
        {
            return null;
        }

        return expectedUrl;
    }

    public static string? BuildAdminEditUrl(
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

        var sitePath = wordpressSiteUri.AbsolutePath.TrimEnd('/');
        var builder = new UriBuilder(wordpressSiteUri)
        {
            Path = $"{sitePath}/wp-admin/post.php",
            Query = $"post={postId.ToString(CultureInfo.InvariantCulture)}&action=edit",
            Fragment = string.Empty
        };
        var adminEditUrl = builder.Uri.AbsoluteUri;

        return adminEditUrl.Length <= MaxUrlLength ? adminEditUrl : null;
    }

    public static string? Resolve(
        string? externalResourceUrl,
        string? wordpressSiteUrl,
        string? externalResourceId) =>
        NormalizeAdminEditUrl(
            externalResourceUrl,
            wordpressSiteUrl,
            externalResourceId) ??
        BuildAdminEditUrl(wordpressSiteUrl, externalResourceId);

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
}

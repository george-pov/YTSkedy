namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// WordPress-specific <see cref="PublishSettings"/> for a WordPress
/// <see cref="Platform"/>. Carries the site URL and Application Password used
/// by the WordPress REST API. This type is secret-bearing and must be redacted
/// from HTTP responses and sanitized before publication snapshots are written.
/// </summary>
public sealed record WordPressSettings : PublishSettings
{
    public static readonly IReadOnlyList<string> AllowedPostStatuses =
        ["publish", "draft"];

    public WordPressSettings(
        string siteUrl,
        string username,
        string applicationPassword,
        string postStatus)
    {
        if (!IsValidSiteUrl(siteUrl))
        {
            throw new ArgumentException(
                "Site URL must be an absolute HTTP(S) URL. Non-local URLs must use HTTPS.",
                nameof(siteUrl));
        }

        if (!IsValidUsername(username))
        {
            throw new ArgumentException(
                "Username must be non-empty.",
                nameof(username));
        }

        if (!IsValidApplicationPassword(applicationPassword))
        {
            throw new ArgumentException(
                "Application Password must be non-empty.",
                nameof(applicationPassword));
        }

        if (!IsValidPostStatus(postStatus))
        {
            throw new ArgumentException(
                "Post status must be 'publish' or 'draft'.",
                nameof(postStatus));
        }

        SiteUrl = siteUrl.Trim();
        Username = username;
        ApplicationPassword = applicationPassword;
        PostStatus = postStatus;
    }

    public string SiteUrl { get; }

    public string Username { get; }

    public string ApplicationPassword { get; }

    public string PostStatus { get; }

    public static bool IsValidSiteUrl(string? siteUrl)
    {
        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(siteUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not "http" and not "https")
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        return uri.Scheme == "https" || IsLocalHost(uri.Host);
    }

    public static bool IsValidUsername(string? username) =>
        !string.IsNullOrWhiteSpace(username);

    public static bool IsValidApplicationPassword(string? applicationPassword) =>
        !string.IsNullOrWhiteSpace(applicationPassword);

    public static bool IsValidPostStatus(string? postStatus) =>
        postStatus is not null &&
        AllowedPostStatuses.Contains(postStatus, StringComparer.Ordinal);

    private static bool IsLocalHost(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        host == "127.0.0.1";
}

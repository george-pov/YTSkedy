namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// WordPress-specific <see cref="PublishSettings"/> for a WordPress
/// <see cref="Platform"/>. Carries the site URL and Application Password used
/// by the WordPress REST API. This type is secret-bearing and must be redacted
/// from HTTP responses and sanitized before publication snapshots are written.
/// </summary>
public sealed record WordPressSettings : PublishSettings
{
    public const string ScheduledPostStatus = "future";

    public const int MaxScheduleOffsetHours = 168;

    public static readonly IReadOnlyList<string> AllowedPostStatuses =
        ["draft", "pending", "private", ScheduledPostStatus, "publish"];

    public WordPressSettings(
        string siteUrl,
        string username,
        string applicationPassword,
        string postStatus,
        IReadOnlyList<long> categoryIds,
        bool sticky = false,
        int? scheduleOffsetHours = null)
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
                "Post status must be 'draft', 'pending', 'private', 'future', or 'publish'.",
                nameof(postStatus));
        }

        if (!AreValidCategoryIds(categoryIds))
        {
            throw new ArgumentException(
                "Category IDs must contain distinct positive integers.",
                nameof(categoryIds));
        }

        switch (ValidateScheduleOffsetHours(postStatus, scheduleOffsetHours))
        {
            case WordPressScheduleOffsetValidationResult.Valid:
                break;
            case WordPressScheduleOffsetValidationResult.Missing:
                throw new ArgumentException(
                    "Schedule offset hours are required for scheduled posts.",
                    nameof(scheduleOffsetHours));
            case WordPressScheduleOffsetValidationResult.Unsupported:
                throw new ArgumentException(
                    "Schedule offset hours are only supported for scheduled posts.",
                    nameof(scheduleOffsetHours));
            case WordPressScheduleOffsetValidationResult.NonPositive:
            case WordPressScheduleOffsetValidationResult.AboveMaximum:
                throw new ArgumentOutOfRangeException(
                    nameof(scheduleOffsetHours),
                    scheduleOffsetHours,
                    $"Schedule offset hours must be between 1 and {MaxScheduleOffsetHours}.");
            default:
                throw new ArgumentOutOfRangeException(nameof(scheduleOffsetHours));
        }

        SiteUrl = siteUrl.Trim();
        Username = username;
        ApplicationPassword = applicationPassword;
        PostStatus = postStatus;
        CategoryIds = categoryIds.ToArray();
        Sticky = sticky;
        ScheduleOffsetHours = scheduleOffsetHours;
    }

    public string SiteUrl { get; }

    public string Username { get; }

    public string ApplicationPassword { get; }

    public string PostStatus { get; }

    public IReadOnlyList<long> CategoryIds { get; }

    public bool Sticky { get; }

    public int? ScheduleOffsetHours { get; }

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

    public static bool AreValidCategoryIds(IReadOnlyList<long>? categoryIds)
    {
        if (categoryIds is null)
        {
            return false;
        }

        var seen = new HashSet<long>();
        return categoryIds.All(categoryId => categoryId > 0 && seen.Add(categoryId));
    }

    public static bool RequiresScheduleOffsetHours(string? postStatus) =>
        string.Equals(postStatus, ScheduledPostStatus, StringComparison.Ordinal);

    public static WordPressScheduleOffsetValidationResult ValidateScheduleOffsetHours(
        string? postStatus,
        int? scheduleOffsetHours)
    {
        if (!RequiresScheduleOffsetHours(postStatus))
        {
            return scheduleOffsetHours is null
                ? WordPressScheduleOffsetValidationResult.Valid
                : WordPressScheduleOffsetValidationResult.Unsupported;
        }

        if (scheduleOffsetHours is null)
        {
            return WordPressScheduleOffsetValidationResult.Missing;
        }

        if (scheduleOffsetHours <= 0)
        {
            return WordPressScheduleOffsetValidationResult.NonPositive;
        }

        if (scheduleOffsetHours > MaxScheduleOffsetHours)
        {
            return WordPressScheduleOffsetValidationResult.AboveMaximum;
        }

        return WordPressScheduleOffsetValidationResult.Valid;
    }

    public bool TryGetScheduledPostUtc(
        DateTimeOffset scheduledStartUtc,
        out DateTimeOffset scheduledPostUtc)
    {
        scheduledPostUtc = default;
        return RequiresScheduleOffsetHours(PostStatus) &&
            ScheduleOffsetHours is not null &&
            TryComputeScheduledPostUtc(
                scheduledStartUtc,
                ScheduleOffsetHours.Value,
                out scheduledPostUtc);
    }

    public static bool TryComputeScheduledPostUtc(
        DateTimeOffset scheduledStartUtc,
        int scheduleOffsetHours,
        out DateTimeOffset scheduledPostUtc)
    {
        scheduledPostUtc = default;

        if (ValidateScheduleOffsetHours(ScheduledPostStatus, scheduleOffsetHours) !=
            WordPressScheduleOffsetValidationResult.Valid)
        {
            return false;
        }

        try
        {
            scheduledPostUtc =
                scheduledStartUtc - TimeSpan.FromHours(scheduleOffsetHours);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static bool IsLocalHost(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        host == "127.0.0.1";
}

public enum WordPressScheduleOffsetValidationResult
{
    Valid,
    Missing,
    Unsupported,
    NonPositive,
    AboveMaximum
}

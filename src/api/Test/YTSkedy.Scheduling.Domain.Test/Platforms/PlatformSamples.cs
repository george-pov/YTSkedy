using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

internal static class PlatformSamples
{
    public const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    public const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    public const string ExternalResourceId = "abc123youtubeid";

    public static readonly DateTimeOffset PublishedUtc =
        new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset UpdatedUtc =
        new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    public static YouTubeCredentials YouTubeCredentials(
        string clientId = "client-id",
        string clientSecret = "client-secret",
        string refreshToken = "refresh-token") =>
        new(clientId, clientSecret, refreshToken);

    public static YouTubeSettings YouTubeSettings(
        string clientId = "client-id",
        string clientSecret = "client-secret",
        string refreshToken = "refresh-token",
        string privacyStatus = "private",
        bool selfDeclaredMadeForKids = false) =>
        new(
            YouTubeCredentials(clientId, clientSecret, refreshToken),
            privacyStatus,
            selfDeclaredMadeForKids);

    public static WordPressSettings WordPressSettings(
        string siteUrl = "https://example.com/",
        string username = "editor",
        string applicationPassword = "application-password",
        string postStatus = "publish") =>
        new(siteUrl, username, applicationPassword, postStatus);

    public static PublishingContent PublishingContent(
        string titleTemplateId = "title-template",
        string descriptionTemplateId = "description-template") =>
        new(titleTemplateId, descriptionTemplateId);

    public static PlatformView PlatformView(
        string platformId = PlatformId,
        string name = "Main YouTube channel",
        string? referenceKey = null,
        PlatformType type = PlatformType.YouTube,
        PublishSettings? publishSettings = null,
        PublishingContent? publishingContent = null) =>
        new(
            platformId,
            name,
            referenceKey,
            type,
            publishSettings ?? YouTubeSettings(),
            publishingContent ?? PublishingContent());

    public static PlatformPublication PlatformPublication(
        PublishStatus status = PublishStatus.Published,
        string calendarEventId = CalendarEventId,
        string platformId = PlatformId,
        string platformName = "Main YouTube channel",
        PlatformType platformType = PlatformType.YouTube,
        string? externalResourceId = ExternalResourceId,
        DateTimeOffset? publishedUtc = null,
        DateTimeOffset? platformDeletedUtc = null,
        DateTimeOffset? updatedUtc = null,
        PublicationTargetSnapshot? targetSnapshot = null,
        ContentSnapshot? contentSnapshot = null,
        ThumbnailPublishStatus? thumbnailStatus = null) =>
        new(
            calendarEventId,
            platformId,
            platformName,
            platformType,
            status,
            externalResourceId,
            publishedUtc ?? PublishedUtc,
            platformDeletedUtc,
            updatedUtc ?? UpdatedUtc,
            targetSnapshot,
            contentSnapshot,
            thumbnailStatus);
}

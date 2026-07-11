using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

internal static class PlatformSamples
{
    public const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    public const string PlatformId = SchedulingSampleIds.PlatformId;
    public const string ExternalResourceId = SchedulingSampleIds.ExternalResourceId;

    public static readonly DateTimeOffset PublishedUtc = SchedulingSampleTimes.PublishedUtc;

    public static readonly DateTimeOffset UpdatedUtc = SchedulingSampleTimes.UpdatedUtc;

    public static YouTubeCredentials YouTubeCredentials(
        string clientId = "client-id",
        string clientSecret = "client-secret",
        string refreshToken = "refresh-token") =>
        SchedulingSamples.YouTubeCredentials(clientId, clientSecret, refreshToken);

    public static YouTubeSettings YouTubeSettings(
        string clientId = "client-id",
        string clientSecret = "client-secret",
        string refreshToken = "refresh-token",
        string privacyStatus = "private",
        bool selfDeclaredMadeForKids = false) =>
        SchedulingSamples.YouTubeSettings(
            clientId,
            clientSecret,
            refreshToken,
            privacyStatus,
            selfDeclaredMadeForKids);

    public static WordPressSettings WordPressSettings(
        string siteUrl = "https://example.com/",
        string username = "editor",
        string applicationPassword = "application-password",
        string postStatus = "publish",
        IReadOnlyList<long>? categoryIds = null,
        bool sticky = false,
        int? scheduleOffsetHours = null) =>
        SchedulingSamples.WordPressSettings(
            siteUrl,
            username,
            applicationPassword,
            postStatus,
            categoryIds,
            sticky,
            scheduleOffsetHours);

    public static PublishingContent PublishingContent(
        string titleTemplateId = "title-template",
        string descriptionTemplateId = "description-template") =>
        SchedulingSamples.PublishingContent(titleTemplateId, descriptionTemplateId);

    public static PlatformView PlatformView(
        string platformId = PlatformId,
        string name = "Main YouTube channel",
        string? referenceKey = null,
        PlatformType type = PlatformType.YouTube,
        PublishSettings? publishSettings = null,
        PublishingContent? publishingContent = null) =>
        SchedulingSamples.Platform(
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
        SchedulingSamples.Publication(
            status,
            calendarEventId,
            platformId,
            platformName,
            platformType,
            externalResourceId,
            publishedUtc ?? PublishedUtc,
            platformDeletedUtc,
            updatedUtc ?? UpdatedUtc,
            targetSnapshot,
            contentSnapshot,
            thumbnailStatus);
}

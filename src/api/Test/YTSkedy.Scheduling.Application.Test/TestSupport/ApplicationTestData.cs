using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;
using YTSkedy.Scheduling.TestSupport;

namespace YTSkedy.Scheduling.Application.Test;

internal static class ApplicationTestData
{
    public const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    public const string PlatformId = SchedulingSampleIds.PlatformId;
    public const string OtherPlatformId = SchedulingSampleIds.OtherPlatformId;
    public const string YouTubePlatformId = SchedulingSampleIds.YouTubePlatformId;
    public const string TitleFieldKey = SchedulingSampleIds.TitleFieldKey;
    public const string DescriptionFieldKey = SchedulingSampleIds.DescriptionFieldKey;
    public const string TitleTemplateId = SchedulingSampleIds.TitleTemplateId;
    public const string DescriptionTemplateId = SchedulingSampleIds.DescriptionTemplateId;
    public const string WordPressTitleTemplateId = SchedulingSampleIds.WordPressTitleTemplateId;
    public const string WordPressDescriptionTemplateId = SchedulingSampleIds.WordPressDescriptionTemplateId;
    public const string YouTubeClientId = SchedulingSampleIds.YouTubeClientId;
    public const string YouTubeClientSecret = SchedulingSampleIds.YouTubeClientSecret;
    public const string YouTubeRefreshToken = SchedulingSampleIds.YouTubeRefreshToken;

    public static readonly DateTimeOffset Now = SchedulingSampleTimes.Now;

    public static readonly DateTimeOffset FutureStart = SchedulingSampleTimes.FutureStart;

    public static ScheduledStart ScheduledStart(
        DateTime? localStart = null,
        string timeZoneId = "America/Vancouver") =>
        SchedulingSamples.ScheduledStart(localStart, timeZoneId);

    public static EventTextFields TextFields() =>
        SchedulingSamples.TextFields();

    public static EventTextSnapshot Text(
        string? title = "English title",
        string? description = "English description") =>
        SchedulingSamples.Text(title, description);

    public static CalendarEventView CalendarEvent(
        string calendarEventId = CalendarEventId,
        DateTimeOffset? scheduledStartUtc = null,
        ScheduledStart? start = null,
        EventTextSnapshot? text = null) =>
        SchedulingSamples.CalendarEvent(
            calendarEventId,
            scheduledStartUtc,
            start,
            text);

    public static YouTubeSettings YouTubeSettings(
        string privacyStatus = "private",
        bool selfDeclaredMadeForKids = false) =>
        SchedulingSamples.YouTubeSettings(
            privacyStatus: privacyStatus,
            selfDeclaredMadeForKids: selfDeclaredMadeForKids);

    public static WordPressSettings WordPressSettings(
        string postStatus = "publish") =>
        SchedulingSamples.WordPressSettings(postStatus: postStatus);

    public static PublishingContent PublishingContent(
        string titleTemplateId = TitleTemplateId,
        string descriptionTemplateId = DescriptionTemplateId) =>
        SchedulingSamples.PublishingContent(titleTemplateId, descriptionTemplateId);

    public static PlatformView Platform(
        string platformId = PlatformId,
        string? name = null,
        string? referenceKey = null,
        PlatformType type = PlatformType.YouTube,
        PublishSettings? publishSettings = null,
        PublishingContent? publishingContent = null) =>
        SchedulingSamples.Platform(
            platformId,
            name,
            referenceKey,
            type,
            publishSettings,
            publishingContent);

    public static TemplateView Template(
        string id,
        TemplateType type,
        string content,
        string name = "Template") =>
        SchedulingSamples.Template(id, type, content, name);

    public static IReadOnlyList<TemplateView> RequiredTemplates() =>
        SchedulingSamples.RequiredTemplates();

    public static PlatformPublication Publication(
        PublishStatus status,
        string calendarEventId = CalendarEventId,
        string platformId = PlatformId,
        string platformName = "Main YouTube channel",
        PlatformType platformType = PlatformType.YouTube,
        string? externalResourceId = null,
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
            publishedUtc,
            platformDeletedUtc,
            updatedUtc,
            targetSnapshot,
            contentSnapshot,
            thumbnailStatus);

    public static Thumbnail Thumbnail(
        string calendarEventId = CalendarEventId,
        string fileName = "stream.png",
        string contentType = "image/png",
        long sizeBytes = 123,
        int width = 1280,
        int height = 720,
        DateTimeOffset? updatedUtc = null,
        string? blobName = null) =>
        SchedulingSamples.Thumbnail(
            calendarEventId,
            fileName,
            contentType,
            sizeBytes,
            width,
            height,
            updatedUtc,
            blobName);

    public static ThumbnailContent ThumbnailContent(
        byte[]? content = null,
        string contentType = "image/png") =>
        new(content ?? [1, 2, 3], contentType);

    public static string ThumbnailBlobName(string calendarEventId = CalendarEventId) =>
        SchedulingSamples.ThumbnailBlobName(calendarEventId);
}

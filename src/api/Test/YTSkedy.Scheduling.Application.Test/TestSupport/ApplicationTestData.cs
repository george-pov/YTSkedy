using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

internal static class ApplicationTestData
{
    public const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    public const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    public const string OtherPlatformId = "8c1d77e0c0a04b2bb0d6f7a9e2c31845";
    public const string YouTubePlatformId = "6ab4a32f3f344de1a7c3a9f4a2f94918";
    public const string TitleFieldKey = "text1";
    public const string DescriptionFieldKey = "text2";
    public const string TitleTemplateId = "title-template";
    public const string DescriptionTemplateId = "description-template";
    public const string WordPressTitleTemplateId = "wordpress-title-template";
    public const string WordPressDescriptionTemplateId = "wordpress-description-template";
    public const string YouTubeClientId = "client-id";
    public const string YouTubeClientSecret = "client-secret";
    public const string YouTubeRefreshToken = "refresh-token";

    public static readonly DateTimeOffset Now =
        new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset FutureStart =
        new(2026, 6, 25, 17, 0, 0, TimeSpan.Zero);

    public static ScheduledStart ScheduledStart(
        DateTime? localStart = null,
        string timeZoneId = "America/Vancouver") =>
        new(localStart ?? new DateTime(2026, 6, 25, 10, 0, 0), timeZoneId);

    public static EventTextFields TextFields() =>
        new(
            [
                new EventTextField("Title", EventTextType.ShortText, 50),
                new EventTextField("Description", EventTextType.LongText, 2500)
            ]);

    public static EventTextSnapshot Text(
        string? title = "English title",
        string? description = "English description")
    {
        var values = new List<EventTextValue>();
        if (title is not null)
        {
            values.Add(new EventTextValue(TitleFieldKey, title));
        }

        values.Add(new EventTextValue(DescriptionFieldKey, description ?? string.Empty));

        return new EventTextSnapshot(
            [
                new EventTextField("Title", EventTextType.ShortText, 50)
                {
                    FieldKey = TitleFieldKey
                },
                new EventTextField("Description", EventTextType.LongText, 2500)
                {
                    FieldKey = DescriptionFieldKey
                }
            ],
            values);
    }

    public static CalendarEventView CalendarEvent(
        string calendarEventId = CalendarEventId,
        DateTimeOffset? scheduledStartUtc = null,
        ScheduledStart? start = null,
        EventTextSnapshot? text = null) =>
        new(
            calendarEventId,
            start ?? ScheduledStart(),
            scheduledStartUtc ?? FutureStart,
            text ?? Text());

    public static YouTubeSettings YouTubeSettings(
        string privacyStatus = "private",
        bool selfDeclaredMadeForKids = false) =>
        new(
            new YouTubeCredentials(YouTubeClientId, YouTubeClientSecret, YouTubeRefreshToken),
            privacyStatus,
            selfDeclaredMadeForKids);

    public static WordPressSettings WordPressSettings(
        string postStatus = "publish") =>
        new("https://blog.example.test/", "publisher", "application-password", postStatus);

    public static PublishingContent PublishingContent(
        string titleTemplateId = TitleTemplateId,
        string descriptionTemplateId = DescriptionTemplateId) =>
        new(titleTemplateId, descriptionTemplateId);

    public static PlatformView Platform(
        string platformId = PlatformId,
        string? name = null,
        string? referenceKey = null,
        PlatformType type = PlatformType.YouTube,
        PublishSettings? publishSettings = null,
        PublishingContent? publishingContent = null) =>
        new(
            platformId,
            name ?? (type == PlatformType.YouTube ? "Main YouTube channel" : "Company blog"),
            referenceKey,
            type,
            publishSettings ?? DefaultPublishSettings(type),
            publishingContent ?? PublishingContent());

    public static TemplateView Template(
        string id,
        TemplateType type,
        string content,
        string name = "Template") =>
        new(id, name, type, content);

    public static IReadOnlyList<TemplateView> RequiredTemplates() =>
    [
        Template(TitleTemplateId, TemplateType.YouTube, "{{ text1 }}", "Title"),
        Template(DescriptionTemplateId, TemplateType.YouTube, "{{ text2 }}", "Description"),
        Template(TitleTemplateId, TemplateType.WordPress, "{{ text1 }}", "Title"),
        Template(DescriptionTemplateId, TemplateType.WordPress, "{{ text2 }}", "Description")
    ];

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
        new(
            calendarEventId,
            platformId,
            platformName,
            platformType,
            status,
            externalResourceId,
            publishedUtc,
            platformDeletedUtc,
            updatedUtc ?? Now,
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
        new(
            fileName,
            contentType,
            sizeBytes,
            width,
            height,
            updatedUtc ?? Now,
            blobName ?? ThumbnailBlobName(calendarEventId));

    public static ThumbnailContent ThumbnailContent(
        byte[]? content = null,
        string contentType = "image/png") =>
        new(content ?? [1, 2, 3], contentType);

    public static string ThumbnailBlobName(string calendarEventId = CalendarEventId) =>
        $"calendar-events/{calendarEventId}/thumbnail";

    private static PublishSettings DefaultPublishSettings(PlatformType type) =>
        type == PlatformType.WordPress ? WordPressSettings() : YouTubeSettings();
}

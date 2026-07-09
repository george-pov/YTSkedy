using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.TestSupport;

public static class SchedulingSamples
{
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
            values.Add(new EventTextValue(SchedulingSampleIds.TitleFieldKey, title));
        }

        values.Add(new EventTextValue(
            SchedulingSampleIds.DescriptionFieldKey,
            description ?? string.Empty));

        return new EventTextSnapshot(
            [
                new EventTextField("Title", EventTextType.ShortText, 50)
                {
                    FieldKey = SchedulingSampleIds.TitleFieldKey
                },
                new EventTextField("Description", EventTextType.LongText, 2500)
                {
                    FieldKey = SchedulingSampleIds.DescriptionFieldKey
                }
            ],
            values);
    }

    public static CalendarEventView CalendarEvent(
        string calendarEventId = SchedulingSampleIds.CalendarEventId,
        DateTimeOffset? scheduledStartUtc = null,
        ScheduledStart? start = null,
        EventTextSnapshot? text = null) =>
        new(
            calendarEventId,
            start ?? ScheduledStart(),
            scheduledStartUtc ?? SchedulingSampleTimes.FutureStart,
            text ?? Text());

    public static YouTubeCredentials YouTubeCredentials(
        string clientId = SchedulingSampleIds.YouTubeClientId,
        string clientSecret = SchedulingSampleIds.YouTubeClientSecret,
        string refreshToken = SchedulingSampleIds.YouTubeRefreshToken) =>
        new(clientId, clientSecret, refreshToken);

    public static YouTubeSettings YouTubeSettings(
        string clientId = SchedulingSampleIds.YouTubeClientId,
        string clientSecret = SchedulingSampleIds.YouTubeClientSecret,
        string refreshToken = SchedulingSampleIds.YouTubeRefreshToken,
        string privacyStatus = "private",
        bool selfDeclaredMadeForKids = false) =>
        new(
            YouTubeCredentials(clientId, clientSecret, refreshToken),
            privacyStatus,
            selfDeclaredMadeForKids);

    public static WordPressSettings WordPressSettings(
        string siteUrl = "https://blog.example.test/",
        string username = "publisher",
        string applicationPassword = "application-password",
        string postStatus = "publish",
        bool sticky = false,
        int? scheduleOffsetHours = null) =>
        new(siteUrl, username, applicationPassword, postStatus, sticky, scheduleOffsetHours);

    public static PublishingContent PublishingContent(
        string titleTemplateId = SchedulingSampleIds.TitleTemplateId,
        string descriptionTemplateId = SchedulingSampleIds.DescriptionTemplateId) =>
        new(titleTemplateId, descriptionTemplateId);

    public static PlatformView Platform(
        string platformId = SchedulingSampleIds.PlatformId,
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
        Template(SchedulingSampleIds.TitleTemplateId, TemplateType.YouTube, "{{ text1 }}", "Title"),
        Template(
            SchedulingSampleIds.DescriptionTemplateId,
            TemplateType.YouTube,
            "{{ text2 }}",
            "Description"),
        Template(
            SchedulingSampleIds.TitleTemplateId,
            TemplateType.WordPress,
            "{{ text1 }}",
            "Title"),
        Template(
            SchedulingSampleIds.DescriptionTemplateId,
            TemplateType.WordPress,
            "{{ text2 }}",
            "Description")
    ];

    public static PlatformPublication Publication(
        PublishStatus status = PublishStatus.Published,
        string calendarEventId = SchedulingSampleIds.CalendarEventId,
        string platformId = SchedulingSampleIds.PlatformId,
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
            updatedUtc ?? SchedulingSampleTimes.Now,
            targetSnapshot,
            contentSnapshot,
            thumbnailStatus);

    public static Thumbnail Thumbnail(
        string calendarEventId = SchedulingSampleIds.CalendarEventId,
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
            updatedUtc ?? SchedulingSampleTimes.Now,
            blobName ?? ThumbnailBlobName(calendarEventId));

    public static string ThumbnailBlobName(
        string calendarEventId = SchedulingSampleIds.CalendarEventId) =>
        $"calendar-events/{calendarEventId}/thumbnail";

    private static PublishSettings DefaultPublishSettings(PlatformType type) =>
        type == PlatformType.WordPress ? WordPressSettings() : YouTubeSettings();
}

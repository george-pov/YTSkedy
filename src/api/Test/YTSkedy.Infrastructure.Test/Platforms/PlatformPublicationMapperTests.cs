using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Scheduling.TestSupport;

namespace YTSkedy.Infrastructure.Test.Platforms;

public class PlatformPublicationMapperTests
{
    private const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    private const string PlatformId = SchedulingSampleIds.PlatformId;

    [Fact]
    public void ToPublication_PublishedEntity_MapsEveryField()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var updatedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);
        var entity = CreateEntity(PublishStatus.Published);
        entity.ExternalResourceId = "abc123youtubeid";
        entity.PublishedUtc = publishedUtc;
        entity.UpdatedUtc = updatedUtc;
        entity.ContentSnapshotTitle = "Rendered title";
        entity.ContentSnapshotDescription = "Rendered description";
        entity.ThumbnailStatus = "Applied";

        var publication = PlatformPublicationMapper.ToPublication(entity);

        Assert.Equal(CalendarEventId, publication.CalendarEventId);
        Assert.Equal(PlatformId, publication.PlatformId);
        Assert.Equal("Main YouTube channel", publication.PlatformName);
        Assert.Equal(PlatformType.YouTube, publication.PlatformType);
        Assert.Equal(PublishStatus.Published, publication.Status);
        Assert.Equal("abc123youtubeid", publication.ExternalResourceId);
        Assert.Equal(ThumbnailPublishStatus.Applied, publication.ThumbnailStatus);
        Assert.Equal(publishedUtc, publication.PublishedUtc);
        Assert.Null(publication.PlatformDeletedUtc);
        Assert.Equal(updatedUtc, publication.UpdatedUtc);
        Assert.NotNull(publication.TargetSnapshot);
        Assert.Equal(PlatformType.YouTube, publication.TargetSnapshot!.PlatformType);
        Assert.Equal("client-id", publication.TargetSnapshot.YouTubeClientId);
        Assert.NotNull(publication.ContentSnapshot);
        Assert.Equal("Rendered title", publication.ContentSnapshot!.Title);
        Assert.Equal("Rendered description", publication.ContentSnapshot.Description);
        Assert.False(publication.IsOrphaned);
    }

    [Fact]
    public void ToPublication_OrphanedEntity_IsOrphaned()
    {
        var deletedUtc = new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);
        var entity = CreateEntity(PublishStatus.Published);
        entity.PlatformDeletedUtc = deletedUtc;

        var publication = PlatformPublicationMapper.ToPublication(entity);

        Assert.Equal(deletedUtc, publication.PlatformDeletedUtc);
        Assert.True(publication.IsOrphaned);
    }

    [Fact]
    public void ToPublication_UnknownStoredStatus_Throws()
    {
        var entity = CreateEntity(PublishStatus.Publishing);
        entity.Status = "Garbled";

        Assert.Throws<InvalidOperationException>(() => PlatformPublicationMapper.ToPublication(entity));
    }

    [Fact]
    public void ToPublications_DifferentStatuses_MapsEachIndependently()
    {
        var publishing = CreateEntity(PublishStatus.Publishing);
        publishing.PlatformId = PlatformId;
        var published = CreateEntity(PublishStatus.Published);
        published.PlatformId = SchedulingSampleIds.OtherPlatformId;

        var publications = PlatformPublicationMapper.ToPublications([publishing, published]);

        Assert.Collection(
            publications,
            first => Assert.Equal(PublishStatus.Publishing, first.Status),
            second => Assert.Equal(PublishStatus.Published, second.Status));
    }

    [Fact]
    public void ToPublishingEntity_BuildsPublishingRowWithCopiedPlatformDetails()
    {
        var now = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var attempt = new PlatformPublicationAttempt(
            CalendarEventId,
            PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            new YouTubeSettings(Credentials(), "private", false),
            new ContentSnapshot("Rendered title", "Rendered description"));

        var entity = PlatformPublicationMapper.ToPublishingEntity(attempt, now);

        Assert.Equal("event-f81d4fae7dec11d0a76500a0c91e6bf6", entity.PartitionKey);
        Assert.Equal("platform-4fb4a32f3f344de1a7c3a9f4a2f94918", entity.RowKey);
        Assert.Equal(CalendarEventId, entity.CalendarEventId);
        Assert.Equal(PlatformId, entity.PlatformId);
        Assert.Equal("Main YouTube channel", entity.PlatformName);
        Assert.Equal("YouTube", entity.PlatformType);
        Assert.Equal("Publishing", entity.Status);
        Assert.Null(entity.ExternalResourceId);
        Assert.Equal("NotConfigured", entity.ThumbnailStatus);
        Assert.Equal("Rendered title", entity.ContentSnapshotTitle);
        Assert.Equal("Rendered description", entity.ContentSnapshotDescription);
        Assert.Null(entity.PublishedUtc);
        Assert.Null(entity.PlatformDeletedUtc);
        Assert.Equal(now, entity.CreatedUtc);
        Assert.Equal(now, entity.UpdatedUtc);
    }

    [Fact]
    public void ToPublishingEntity_SerializesPublishSettingsThatRoundTrip()
    {
        var attempt = new PlatformPublicationAttempt(
            CalendarEventId,
            PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            new YouTubeSettings(Credentials(), "unlisted", true),
            new ContentSnapshot("Rendered title", null));

        var entity = PlatformPublicationMapper.ToPublishingEntity(
            attempt,
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));

        Assert.Contains("\"clientId\":\"client-id\"", entity.PublishSettingsJson);
        Assert.Contains("\"clientSecretConfigured\":true", entity.PublishSettingsJson);
        Assert.Contains("\"refreshTokenConfigured\":true", entity.PublishSettingsJson);
        Assert.Contains("\"privacyStatus\":\"unlisted\"", entity.PublishSettingsJson);
        Assert.Contains("\"selfDeclaredMadeForKids\":true", entity.PublishSettingsJson);
        Assert.DoesNotContain("\"clientSecret\":\"", entity.PublishSettingsJson);
        Assert.DoesNotContain("\"refreshToken\":\"", entity.PublishSettingsJson);
        Assert.DoesNotContain("client-secret", entity.PublishSettingsJson);
        Assert.DoesNotContain("refresh-token", entity.PublishSettingsJson);
    }

    [Fact]
    public void ToPublishingEntity_WordPressSettings_OmitsApplicationPasswordFromSnapshot()
    {
        var attempt = new PlatformPublicationAttempt(
            CalendarEventId,
            PlatformId,
            "Main WordPress site",
            PlatformType.WordPress,
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "publish"),
            new ContentSnapshot("Rendered title", null));

        var entity = PlatformPublicationMapper.ToPublishingEntity(
            attempt,
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal("WordPress", entity.PlatformType);
        Assert.Null(entity.ThumbnailStatus);
        Assert.Contains("\"siteUrl\":\"https://example.com\"", entity.PublishSettingsJson);
        Assert.Contains("\"username\":\"editor\"", entity.PublishSettingsJson);
        Assert.Contains("\"postStatus\":\"publish\"", entity.PublishSettingsJson);
        Assert.DoesNotContain("applicationPassword", entity.PublishSettingsJson);
        Assert.DoesNotContain("application-password", entity.PublishSettingsJson);
    }

    [Theory]
    [InlineData("NotPublished", PublishStatus.NotPublished)]
    [InlineData("Publishing", PublishStatus.Publishing)]
    [InlineData("Published", PublishStatus.Published)]
    [InlineData("published", PublishStatus.Published)]
    public void ParseStatus_KnownValue_ParsesCaseInsensitively(string stored, PublishStatus expected)
    {
        Assert.Equal(expected, PlatformPublicationMapper.ParseStatus(stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nonsense")]
    public void ParseStatus_UnknownValue_Throws(string? stored)
    {
        Assert.Throws<InvalidOperationException>(() => PlatformPublicationMapper.ParseStatus(stored));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("NotConfigured", ThumbnailPublishStatus.NotConfigured)]
    [InlineData("Applied", ThumbnailPublishStatus.Applied)]
    [InlineData("failed", ThumbnailPublishStatus.Failed)]
    public void ParseThumbnailStatus_KnownValue_ParsesCaseInsensitively(
        string? stored,
        ThumbnailPublishStatus? expected)
    {
        Assert.Equal(expected, PlatformPublicationMapper.ParseThumbnailStatus(stored));
    }

    [Fact]
    public void ParseThumbnailStatus_UnknownValue_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => PlatformPublicationMapper.ParseThumbnailStatus("nonsense"));
    }

    private static PlatformPublicationEntity CreateEntity(PublishStatus status) =>
        new()
        {
            PartitionKey = PlatformPublicationKey.PartitionKeyFor(CalendarEventId),
            RowKey = PlatformPublicationKey.RowKeyFor(PlatformId),
            CalendarEventId = CalendarEventId,
            PlatformId = PlatformId,
            PlatformName = "Main YouTube channel",
            PlatformType = "YouTube",
            Status = status.ToString(),
            PublishSettingsJson = PublishSettingsSerializer.Serialize(
                PlatformType.YouTube,
                new YouTubeSettings(Credentials(), "private", false)),
            CreatedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            UpdatedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)
        };

    private static YouTubeCredentials Credentials() =>
        SchedulingSamples.YouTubeCredentials();
}

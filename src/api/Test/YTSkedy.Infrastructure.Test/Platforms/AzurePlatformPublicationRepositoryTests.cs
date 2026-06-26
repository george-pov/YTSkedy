using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.Platforms;

public class AzurePlatformPublicationRepositoryTests
{
    [Fact]
    public void CanDeletePublished_PublishedRowWithMatchingExternalId_ReturnsDeleted()
    {
        var entity = PublishedEntity("yt-broadcast-id");

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            "yt-broadcast-id");

        Assert.Equal(DeletePublishedResult.Deleted, result);
    }

    [Fact]
    public void CanDeletePublished_PublishingRow_ReturnsChanged()
    {
        var entity = PublishedEntity("yt-broadcast-id");
        entity.Status = PublishStatus.Publishing.ToString();

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            "yt-broadcast-id");

        Assert.Equal(DeletePublishedResult.Changed, result);
    }

    [Fact]
    public void CanDeletePublished_OrphanedPublishedRow_ReturnsChanged()
    {
        var entity = PublishedEntity("yt-broadcast-id");
        entity.PlatformDeletedUtc = new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            "yt-broadcast-id");

        Assert.Equal(DeletePublishedResult.Changed, result);
    }

    [Fact]
    public void CanDeletePublished_ExternalResourceIdChanged_ReturnsChanged()
    {
        var entity = PublishedEntity("other-resource-id");

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            "yt-broadcast-id");

        Assert.Equal(DeletePublishedResult.Changed, result);
    }

    private static PlatformPublicationEntity PublishedEntity(string externalResourceId) =>
        new()
        {
            PartitionKey = PlatformPublicationKey.PartitionKeyFor("f81d4fae7dec11d0a76500a0c91e6bf6"),
            RowKey = PlatformPublicationKey.RowKeyFor("4fb4a32f3f344de1a7c3a9f4a2f94918"),
            CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6",
            PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918",
            PlatformName = "Main YouTube channel",
            PlatformType = PlatformType.YouTube.ToString(),
            Status = PublishStatus.Published.ToString(),
            ExternalResourceId = externalResourceId,
            PublishSettingsJson = "{}",
            PublishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            CreatedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            UpdatedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)
        };
}

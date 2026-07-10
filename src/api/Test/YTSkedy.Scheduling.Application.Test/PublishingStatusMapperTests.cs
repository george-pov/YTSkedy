using YTSkedy.Scheduling.Application.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class PublishingStatusMapperTests
{
    [Fact]
    public void Map_NoPublishedPlatforms_ReturnsNotPublished()
    {
        var result = PublishingStatusMapper.Map(
            Set(),
            Set("platform-a", "platform-b"));

        Assert.Equal(PublishingStatus.NotPublished, result);
    }

    [Fact]
    public void Map_OneOfMultipleActivePlatformsPublished_ReturnsPartiallyPublished()
    {
        var result = PublishingStatusMapper.Map(
            Set("platform-a"),
            Set("platform-a", "platform-b"));

        Assert.Equal(PublishingStatus.PartiallyPublished, result);
    }

    [Fact]
    public void Map_AllActivePlatformsPublished_ReturnsFullyPublished()
    {
        var result = PublishingStatusMapper.Map(
            Set("platform-a", "platform-b"),
            Set("platform-a", "platform-b"));

        Assert.Equal(PublishingStatus.FullyPublished, result);
    }

    [Fact]
    public void Map_AllActivePlatformsPublishedWithHistoricalIds_ReturnsFullyPublished()
    {
        var result = PublishingStatusMapper.Map(
            Set("deleted-platform", "platform-a", "platform-b"),
            Set("platform-a", "platform-b"));

        Assert.Equal(PublishingStatus.FullyPublished, result);
    }

    [Fact]
    public void Map_NoActivePlatformsWithHistoricalIds_ReturnsPartiallyPublished()
    {
        var result = PublishingStatusMapper.Map(
            Set("deleted-platform"),
            Set());

        Assert.Equal(PublishingStatus.PartiallyPublished, result);
    }

    [Fact]
    public void Map_PlatformIdsDifferOnlyByCase_UsesOrdinalComparison()
    {
        IReadOnlySet<string> publishedPlatformIds = new HashSet<string>(
            ["platform-a"],
            StringComparer.OrdinalIgnoreCase);

        var result = PublishingStatusMapper.Map(
            publishedPlatformIds,
            Set("PLATFORM-A"));

        Assert.Equal(PublishingStatus.PartiallyPublished, result);
    }

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.Ordinal);
}

using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PlatformPublicationTests
{
    [Fact]
    public void IsOrphaned_PlatformDeletedUtcSet_ReturnsTrue()
    {
        var publication = CreatePublication(
            PublishStatus.Published,
            platformDeletedUtc: new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));

        Assert.True(publication.IsOrphaned);
    }

    [Fact]
    public void IsOrphaned_PlatformDeletedUtcNull_ReturnsFalse()
    {
        var publication = CreatePublication(PublishStatus.Published, platformDeletedUtc: null);

        Assert.False(publication.IsOrphaned);
    }

    private static PlatformPublication CreatePublication(
        PublishStatus status,
        DateTimeOffset? platformDeletedUtc) =>
        new(
            "20260615T170000Z",
            "4fb4a32f3f344de1a7c3a9f4a2f94918",
            "Main YouTube channel",
            PlatformType.YouTube,
            status,
            "abc123youtubeid",
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            platformDeletedUtc,
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));
}

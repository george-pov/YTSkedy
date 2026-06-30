using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PlatformPublicationTests
{
    [Fact]
    public void PublishingContent_BlankTemplateIds_NormalizesToNull()
    {
        var content = new PublishingContent("   ", null);

        Assert.Null(content.TitleTemplateId);
        Assert.Null(content.DescriptionTemplateId);
    }

    [Fact]
    public void RenderedContent_WhitespaceDescription_NormalizesToNull()
    {
        var content = new RenderedContent("Rendered title", "   ");

        Assert.Equal("Rendered title", content.Title);
        Assert.Null(content.Description);
    }

    [Fact]
    public void ContentSnapshot_WhitespaceDescription_NormalizesToNull()
    {
        var snapshot = new ContentSnapshot("Published title", "   ");

        Assert.Equal("Published title", snapshot.Title);
        Assert.Null(snapshot.Description);
    }

    [Fact]
    public void ContentSnapshot_WhitespaceTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ContentSnapshot("   ", null));
    }

    [Fact]
    public void Constructor_ContentSnapshotProvided_SetsSnapshot()
    {
        var snapshot = new ContentSnapshot("Published title", "Published description");
        var publication = CreatePublication(
            PublishStatus.Published,
            platformDeletedUtc: null,
            contentSnapshot: snapshot);

        Assert.Same(snapshot, publication.ContentSnapshot);
    }

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
        DateTimeOffset? platformDeletedUtc,
        ContentSnapshot? contentSnapshot = null) =>
        new(
            "f81d4fae7dec11d0a76500a0c91e6bf6",
            "4fb4a32f3f344de1a7c3a9f4a2f94918",
            "Main YouTube channel",
            PlatformType.YouTube,
            status,
            "abc123youtubeid",
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            platformDeletedUtc,
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            ContentSnapshot: contentSnapshot);
}

using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PlatformPublicationTests
{
    [Fact]
    public void PublishingContent_BlankTitleTemplateId_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new PublishingContent("   ", "description-template"));

        Assert.Equal("titleTemplateId", exception.ParamName);
    }

    [Fact]
    public void PublishingContent_ValidTemplateIds_TrimsValues()
    {
        var content = new PublishingContent(" title-template ", " description-template ");

        Assert.Equal("title-template", content.TitleTemplateId);
        Assert.Equal("description-template", content.DescriptionTemplateId);
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
        var publication = PlatformSamples.PlatformPublication(
            PublishStatus.Published,
            platformDeletedUtc: null,
            contentSnapshot: snapshot);

        Assert.Same(snapshot, publication.ContentSnapshot);
    }

    [Fact]
    public void Constructor_ThumbnailStatusProvided_SetsThumbnailStatus()
    {
        var publication = PlatformSamples.PlatformPublication(
            PublishStatus.Published,
            platformDeletedUtc: null,
            thumbnailStatus: ThumbnailPublishStatus.Applied);

        Assert.Equal(ThumbnailPublishStatus.Applied, publication.ThumbnailStatus);
    }

    [Fact]
    public void IsOrphaned_PlatformDeletedUtcSet_ReturnsTrue()
    {
        var publication = PlatformSamples.PlatformPublication(
            PublishStatus.Published,
            platformDeletedUtc: new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));

        Assert.True(publication.IsOrphaned);
    }

    [Fact]
    public void IsOrphaned_PlatformDeletedUtcNull_ReturnsFalse()
    {
        var publication = PlatformSamples.PlatformPublication(
            PublishStatus.Published,
            platformDeletedUtc: null);

        Assert.False(publication.IsOrphaned);
    }
}

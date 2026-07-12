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

    [Theory]
    [MemberData(nameof(ContentWithWhitespaceDescription))]
    public void ContentValue_WhitespaceDescription_NormalizesToNull(
        string title,
        Func<object> createContent,
        Func<object, string?> description)
    {
        var content = createContent();

        Assert.Equal(title, GetTitle(content));
        Assert.Null(description(content));
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsOrphaned_PlatformDeletedUtc_ReturnsExpected(bool hasPlatformDeletedUtc)
    {
        var publication = PlatformSamples.PlatformPublication(
            PublishStatus.Published,
            platformDeletedUtc: hasPlatformDeletedUtc
                ? new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)
                : null);

        Assert.Equal(hasPlatformDeletedUtc, publication.IsOrphaned);
    }

    public static TheoryData<string, Func<object>, Func<object, string?>> ContentWithWhitespaceDescription()
    {
        return new TheoryData<string, Func<object>, Func<object, string?>>
        {
            {
                "Rendered title",
                () => new RenderedContent("Rendered title", "   "),
                content => ((RenderedContent)content).Description
            },
            {
                "Published title",
                () => new ContentSnapshot("Published title", "   "),
                content => ((ContentSnapshot)content).Description
            },
        };
    }

    private static string GetTitle(object content)
    {
        return content switch
        {
            RenderedContent renderedContent => renderedContent.Title,
            ContentSnapshot contentSnapshot => contentSnapshot.Title,
            _ => throw new ArgumentException("Unsupported content type.", nameof(content)),
        };
    }
}

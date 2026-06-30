using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PlatformTests
{
    private static readonly YouTubeSettings Settings =
        new(
            new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
            "private",
            false);

    [Fact]
    public void Constructor_ValidInput_SetsProperties()
    {
        var publishingContent = RequiredPublishingContent();
        var platform = new Platform(
            "Main YouTube channel",
            PlatformType.YouTube,
            Settings,
            publishingContent,
            "main-youtube");

        Assert.Equal("Main YouTube channel", platform.Name);
        Assert.Equal(PlatformType.YouTube, platform.Type);
        Assert.Same(Settings, platform.PublishSettings);
        Assert.Equal("main-youtube", platform.ReferenceKey);
        Assert.Same(publishingContent, platform.PublishingContent);
    }

    [Fact]
    public void Constructor_PublishingContent_SetsProperty()
    {
        var publishingContent = new PublishingContent(
            "title-template",
            "description-template");

        var platform = new Platform(
            "Main YouTube channel",
            PlatformType.YouTube,
            Settings,
            publishingContent,
            "main-youtube");

        Assert.Same(publishingContent, platform.PublishingContent);
    }

    [Fact]
    public void Constructor_NameWithSurroundingWhitespace_IsTrimmed()
    {
        var platform = new Platform(
            "  Main channel  ",
            PlatformType.YouTube,
            Settings,
            publishingContent: RequiredPublishingContent());

        Assert.Equal("Main channel", platform.Name);
    }

    [Fact]
    public void Constructor_NameAtMaxLength_IsAccepted()
    {
        var name = new string('n', Platform.MaxNameLength);

        var platform = new Platform(
            name,
            PlatformType.YouTube,
            Settings,
            publishingContent: RequiredPublishingContent());

        Assert.Equal(name, platform.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(
            () => new Platform(
                name!,
                PlatformType.YouTube,
                Settings,
                publishingContent: RequiredPublishingContent()));
    }

    [Fact]
    public void Constructor_NameAboveMaxLength_Throws()
    {
        var name = new string('n', Platform.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(
            () => new Platform(
                name,
                PlatformType.YouTube,
                Settings,
                publishingContent: RequiredPublishingContent()));
    }

    [Fact]
    public void Constructor_NullPublishSettings_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Platform(
                "Main channel",
                PlatformType.YouTube,
                null!,
                publishingContent: RequiredPublishingContent()));
    }

    [Fact]
    public void Constructor_NullPublishingContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Platform("Main channel", PlatformType.YouTube, Settings, null!));
    }

    [Fact]
    public void Constructor_BlankReferenceKey_SetsNull()
    {
        var platform = new Platform(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            RequiredPublishingContent(),
            "   ");

        Assert.Null(platform.ReferenceKey);
    }

    [Fact]
    public void Constructor_ReferenceKeyWithSurroundingWhitespace_IsTrimmed()
    {
        var platform = new Platform(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            RequiredPublishingContent(),
            "  youTube1  ");

        Assert.Equal("youTube1", platform.ReferenceKey);
    }

    [Fact]
    public void Constructor_ReferenceKeyAtMaxLength_IsAccepted()
    {
        var referenceKey = new string('a', Platform.MaxReferenceKeyLength);

        var platform = new Platform(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            RequiredPublishingContent(),
            referenceKey);

        Assert.Equal(referenceKey, platform.ReferenceKey);
    }

    [Theory]
    [InlineData("with space")]
    [InlineData("with_underscore")]
    [InlineData("with.dot")]
    [InlineData("with/slash")]
    [InlineData("aaaaaaaaaaaaaaaa")]
    public void Constructor_InvalidReferenceKey_Throws(string referenceKey)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Platform(
                "Main channel",
                PlatformType.YouTube,
                Settings,
                RequiredPublishingContent(),
                referenceKey));

        Assert.Equal("referenceKey", exception.ParamName);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Main YouTube channel")]
    public void IsValidName_NonEmptyWithinLimit_ReturnsTrue(string name)
    {
        Assert.True(Platform.IsValidName(name));
    }

    [Fact]
    public void IsValidName_AtMaxLengthAfterTrim_ReturnsTrue()
    {
        var name = $"  {new string('n', Platform.MaxNameLength)}  ";

        Assert.True(Platform.IsValidName(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidName_NullOrWhiteSpace_ReturnsFalse(string? name)
    {
        Assert.False(Platform.IsValidName(name));
    }

    [Fact]
    public void IsValidName_AboveMaxLengthAfterTrim_ReturnsFalse()
    {
        Assert.False(Platform.IsValidName(new string('n', Platform.MaxNameLength + 1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidReferenceKey_NullOrBlank_ReturnsTrue(string? referenceKey)
    {
        Assert.True(Platform.IsValidReferenceKey(referenceKey));
    }

    [Theory]
    [InlineData("youTube1")]
    [InlineData("youtube1")]
    [InlineData("YT-1")]
    [InlineData("abc-123")]
    public void IsValidReferenceKey_LettersDigitsAndHyphenWithinLimit_ReturnsTrue(
        string referenceKey)
    {
        Assert.True(Platform.IsValidReferenceKey(referenceKey));
    }

    [Fact]
    public void ToReferenceKeyLookupValue_MixedCase_ReturnsLowercaseLookupValue()
    {
        Assert.Equal("youtube1", Platform.ToReferenceKeyLookupValue("  youTube1  "));
    }

    private static PublishingContent RequiredPublishingContent() =>
        new("title-template", "description-template");
}

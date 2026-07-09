using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PlatformTests
{
    private static readonly YouTubeSettings Settings =
        PlatformSamples.YouTubeSettings();

    [Fact]
    public void Constructor_ValidInput_SetsProperties()
    {
        var publishingContent = PlatformSamples.PublishingContent();
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
    public void Constructor_NameWithSurroundingWhitespace_IsTrimmed()
    {
        var platform = new Platform(
            "  Main channel  ",
            PlatformType.YouTube,
            Settings,
            publishingContent: PlatformSamples.PublishingContent());

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
            publishingContent: PlatformSamples.PublishingContent());

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
                publishingContent: PlatformSamples.PublishingContent()));
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
                publishingContent: PlatformSamples.PublishingContent()));
    }

    [Fact]
    public void Constructor_NullPublishSettings_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Platform(
                "Main channel",
                PlatformType.YouTube,
                null!,
                publishingContent: PlatformSamples.PublishingContent()));
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
            PlatformSamples.PublishingContent(),
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
            PlatformSamples.PublishingContent(),
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
            PlatformSamples.PublishingContent(),
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
                PlatformSamples.PublishingContent(),
                referenceKey));

        Assert.Equal("referenceKey", exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(ValidNameCases))]
    public void IsValidName_Value_ReturnsExpected(string? name, bool expected)
    {
        var actual = Platform.IsValidName(name);

        Assert.Equal(expected, actual);
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

    public static TheoryData<string?, bool> ValidNameCases => new()
    {
        { "a", true },
        { "Main YouTube channel", true },
        { $"  {new string('n', Platform.MaxNameLength)}  ", true },
        { null, false },
        { "", false },
        { "   ", false },
        { new string('n', Platform.MaxNameLength + 1), false }
    };
}

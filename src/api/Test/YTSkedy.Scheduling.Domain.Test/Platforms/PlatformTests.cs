using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PlatformTests
{
    private static readonly YouTubePublishSettings Settings =
        new("main-youtube-channel", "private", false);

    [Fact]
    public void Constructor_ValidInput_SetsProperties()
    {
        var platform = new Platform("Main YouTube channel", PlatformType.YouTube, Settings);

        Assert.Equal("Main YouTube channel", platform.Name);
        Assert.Equal(PlatformType.YouTube, platform.Type);
        Assert.Same(Settings, platform.PublishSettings);
    }

    [Fact]
    public void Constructor_NameWithSurroundingWhitespace_IsTrimmed()
    {
        var platform = new Platform("  Main channel  ", PlatformType.YouTube, Settings);

        Assert.Equal("Main channel", platform.Name);
    }

    [Fact]
    public void Constructor_NameAtMaxLength_IsAccepted()
    {
        var name = new string('n', Platform.MaxNameLength);

        var platform = new Platform(name, PlatformType.YouTube, Settings);

        Assert.Equal(name, platform.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(
            () => new Platform(name!, PlatformType.YouTube, Settings));
    }

    [Fact]
    public void Constructor_NameAboveMaxLength_Throws()
    {
        var name = new string('n', Platform.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(
            () => new Platform(name, PlatformType.YouTube, Settings));
    }

    [Fact]
    public void Constructor_NullPublishSettings_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Platform("Main channel", PlatformType.YouTube, null!));
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
}

using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class WordPressSettingsTests
{
    [Fact]
    public void Constructor_ValidInput_SetsProperties()
    {
        var settings = new WordPressSettings(
            "  https://example.com/blog  ",
            "editor",
            "application-password",
            "draft");

        Assert.Equal("https://example.com/blog", settings.SiteUrl);
        Assert.Equal("editor", settings.Username);
        Assert.Equal("application-password", settings.ApplicationPassword);
        Assert.Equal("draft", settings.PostStatus);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/wp-json/wp/v2/posts")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("http://example.com")]
    [InlineData("http://user:password@example.com")]
    public void Constructor_InvalidSiteUrl_Throws(string? siteUrl)
    {
        Assert.Throws<ArgumentException>(
            () => new WordPressSettings(siteUrl!, "editor", "password", "publish"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyUsername_Throws(string? username)
    {
        Assert.Throws<ArgumentException>(
            () => new WordPressSettings(
                "https://example.com",
                username!,
                "password",
                "publish"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyApplicationPassword_Throws(string? applicationPassword)
    {
        Assert.Throws<ArgumentException>(
            () => new WordPressSettings(
                "https://example.com",
                "editor",
                applicationPassword!,
                "publish"));
    }

    [Theory]
    [InlineData("Publish")]
    [InlineData("private")]
    [InlineData("")]
    public void Constructor_InvalidPostStatus_Throws(string postStatus)
    {
        Assert.Throws<ArgumentException>(
            () => new WordPressSettings(
                "https://example.com",
                "editor",
                "password",
                postStatus));
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/blog")]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:8000/")]
    [InlineData("http://127.0.0.1:8000/")]
    public void IsValidSiteUrl_AllowedValues_ReturnsTrue(string siteUrl)
    {
        Assert.True(WordPressSettings.IsValidSiteUrl(siteUrl));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("example.com")]
    [InlineData("http://example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("https://user:password@example.com")]
    [InlineData("/relative")]
    public void IsValidSiteUrl_DisallowedValues_ReturnsFalse(string? siteUrl)
    {
        Assert.False(WordPressSettings.IsValidSiteUrl(siteUrl));
    }

    [Fact]
    public void IsValidUsername_NonEmpty_ReturnsTrue()
    {
        Assert.True(WordPressSettings.IsValidUsername("editor"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidUsername_NullOrWhiteSpace_ReturnsFalse(string? username)
    {
        Assert.False(WordPressSettings.IsValidUsername(username));
    }

    [Fact]
    public void IsValidApplicationPassword_NonEmpty_ReturnsTrue()
    {
        Assert.True(WordPressSettings.IsValidApplicationPassword("application-password"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidApplicationPassword_NullOrWhiteSpace_ReturnsFalse(
        string? applicationPassword)
    {
        Assert.False(WordPressSettings.IsValidApplicationPassword(applicationPassword));
    }

    [Theory]
    [InlineData("publish")]
    [InlineData("draft")]
    public void IsValidPostStatus_AllowedLowercaseValues_ReturnsTrue(string postStatus)
    {
        Assert.True(WordPressSettings.IsValidPostStatus(postStatus));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Publish")]
    [InlineData("pending")]
    public void IsValidPostStatus_OtherValues_ReturnsFalse(string? postStatus)
    {
        Assert.False(WordPressSettings.IsValidPostStatus(postStatus));
    }
}

using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class YouTubePublishSettingsTests
{
    [Fact]
    public void Constructor_ValidInput_SetsProperties()
    {
        var settings = new YouTubePublishSettings("main-youtube-channel", "unlisted", true);

        Assert.Equal("main-youtube-channel", settings.Credentials);
        Assert.Equal("unlisted", settings.PrivacyStatus);
        Assert.True(settings.SelfDeclaredMadeForKids);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyCredentials_Throws(string? credentials)
    {
        Assert.Throws<ArgumentException>(
            () => new YouTubePublishSettings(credentials!, "private", false));
    }

    [Theory]
    [InlineData("Private")]
    [InlineData("PUBLIC")]
    [InlineData("bogus")]
    [InlineData("")]
    public void Constructor_InvalidPrivacyStatus_Throws(string privacyStatus)
    {
        Assert.Throws<ArgumentException>(
            () => new YouTubePublishSettings("creds", privacyStatus, false));
    }

    [Theory]
    [InlineData("private")]
    [InlineData("public")]
    [InlineData("unlisted")]
    public void IsValidPrivacyStatus_AllowedLowercaseValues_ReturnsTrue(string privacyStatus)
    {
        Assert.True(YouTubePublishSettings.IsValidPrivacyStatus(privacyStatus));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Private")]
    [InlineData("unlistED")]
    [InlineData("hidden")]
    public void IsValidPrivacyStatus_OtherValues_ReturnsFalse(string? privacyStatus)
    {
        Assert.False(YouTubePublishSettings.IsValidPrivacyStatus(privacyStatus));
    }

    [Fact]
    public void IsValidCredentials_NonEmpty_ReturnsTrue()
    {
        Assert.True(YouTubePublishSettings.IsValidCredentials("main-youtube-channel"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidCredentials_NullOrWhiteSpace_ReturnsFalse(string? credentials)
    {
        Assert.False(YouTubePublishSettings.IsValidCredentials(credentials));
    }
}

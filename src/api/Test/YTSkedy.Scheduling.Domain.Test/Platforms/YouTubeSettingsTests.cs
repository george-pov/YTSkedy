using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class YouTubeSettingsTests
{
    [Fact]
    public void Constructor_ValidInput_SetsProperties()
    {
        var credentials = PlatformSamples.YouTubeCredentials();

        var settings = new YouTubeSettings(
            credentials,
            "unlisted",
            true,
            " 27 ",
            containsSyntheticMedia: true);

        Assert.Same(credentials, settings.Credentials);
        Assert.Equal("unlisted", settings.PrivacyStatus);
        Assert.True(settings.SelfDeclaredMadeForKids);
        Assert.Equal("27", settings.CategoryId);
        Assert.True(settings.ContainsSyntheticMedia);
    }

    [Fact]
    public void Constructor_ExistingCallShape_DefaultsNewSettings()
    {
        var settings = new YouTubeSettings(
            PlatformSamples.YouTubeCredentials(),
            "private",
            false);

        Assert.Null(settings.CategoryId);
        Assert.False(settings.ContainsSyntheticMedia);
    }

    [Fact]
    public void Constructor_NullCategoryAndFalseDisclosure_SetsDefaults()
    {
        var settings = new YouTubeSettings(
            PlatformSamples.YouTubeCredentials(),
            "private",
            false,
            categoryId: null,
            containsSyntheticMedia: false);

        Assert.Null(settings.CategoryId);
        Assert.False(settings.ContainsSyntheticMedia);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankCategory_Throws(string categoryId)
    {
        Assert.Throws<ArgumentException>(
            () => new YouTubeSettings(
                PlatformSamples.YouTubeCredentials(),
                "private",
                false,
                categoryId));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("27", true)]
    [InlineData(" 27 ", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsValidCategoryId_Value_ReturnsExpected(string? categoryId, bool expected)
    {
        Assert.Equal(expected, YouTubeSettings.IsValidCategoryId(categoryId));
    }

    [Fact]
    public void Constructor_NullCredentials_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new YouTubeSettings(null!, "private", false));
    }

    [Theory]
    [InlineData("Private")]
    [InlineData("PUBLIC")]
    [InlineData("bogus")]
    [InlineData("")]
    public void Constructor_InvalidPrivacyStatus_Throws(string privacyStatus)
    {
        Assert.Throws<ArgumentException>(
            () => new YouTubeSettings(
                PlatformSamples.YouTubeCredentials(),
                privacyStatus,
                false));
    }

    [Theory]
    [InlineData("private")]
    [InlineData("public")]
    [InlineData("unlisted")]
    public void IsValidPrivacyStatus_AllowedLowercaseValues_ReturnsTrue(string privacyStatus)
    {
        Assert.True(YouTubeSettings.IsValidPrivacyStatus(privacyStatus));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Private")]
    [InlineData("unlistED")]
    [InlineData("hidden")]
    public void IsValidPrivacyStatus_OtherValues_ReturnsFalse(string? privacyStatus)
    {
        Assert.False(YouTubeSettings.IsValidPrivacyStatus(privacyStatus));
    }

    [Fact]
    public void Credentials_ValidInput_SetsProperties()
    {
        var credentials = new YouTubeCredentials(
            " client-id ",
            "client-secret",
            "refresh-token");

        Assert.Equal("client-id", credentials.ClientId);
        Assert.Equal("client-secret", credentials.ClientSecret);
        Assert.Equal("refresh-token", credentials.RefreshToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Credentials_InvalidClientId_Throws(string? clientId)
    {
        Assert.Throws<ArgumentException>(
            () => new YouTubeCredentials(clientId!, "client-secret", "refresh-token"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Credentials_InvalidClientSecret_Throws(string? clientSecret)
    {
        Assert.Throws<ArgumentException>(
            () => new YouTubeCredentials("client-id", clientSecret!, "refresh-token"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Credentials_InvalidRefreshToken_Throws(string? refreshToken)
    {
        Assert.Throws<ArgumentException>(
            () => new YouTubeCredentials("client-id", "client-secret", refreshToken!));
    }
}

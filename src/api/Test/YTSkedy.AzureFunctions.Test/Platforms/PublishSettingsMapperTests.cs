using System.Text.Json;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using static YTSkedy.AzureFunctions.Test.Platforms.PlatformTestData;

namespace YTSkedy.AzureFunctions.Test.Platforms;

public sealed class PublishSettingsMapperTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RedactSecret_BlankValue_ReturnsNull(string? value)
    {
        var redacted = PublishSettingsMapper.RedactSecret(value, 12, 3, '*');

        Assert.Null(redacted);
    }

    [Fact]
    public void RedactSecret_YouTubePolicy_ReturnsFixedLengthSuffixDisplay()
    {
        var redacted = PublishSettingsMapper.RedactSecret(
            "stored-client-secret-A3B",
            12,
            3,
            '*');

        Assert.NotNull(redacted);
        Assert.Equal("*********A3B", redacted);
        Assert.Equal(12, redacted.Length);
    }

    [Fact]
    public void RedactSecret_YouTubeShortValue_ReturnsFullMask()
    {
        var redacted = PublishSettingsMapper.RedactSecret("AB", 12, 3, '*');

        Assert.Equal("************", redacted);
    }

    [Fact]
    public void RedactSecret_WordPressPolicy_ReturnsFullMask()
    {
        var redacted = PublishSettingsMapper.RedactSecret(
            "local-test-password",
            7,
            0,
            '*');

        Assert.Equal("*******", redacted);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(12, -1)]
    [InlineData(2, 3)]
    public void RedactSecret_InvalidPolicy_ThrowsArgumentOutOfRange(
        int displayLength,
        int visibleSuffixLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PublishSettingsMapper.RedactSecret(
                "stored-client-secret-A3B",
                displayLength,
                visibleSuffixLength,
                '*'));
    }

    [Fact]
    public void ToPublishSettingsResponse_YouTubeSettings_ReturnsSecretDisplayValues()
    {
        var response = PublishSettingsMapper.ToResponse(
            YouTubeSettings(
                clientSecret: "stored-client-secret-A3B",
                refreshToken: "stored-refresh-token-Z9Y"));

        Assert.NotNull(response.Credentials);
        Assert.Equal(SchedulingSampleIds.YouTubeClientId, response.Credentials.ClientId);
        Assert.True(response.Credentials.ClientSecretConfigured);
        Assert.True(response.Credentials.RefreshTokenConfigured);
        Assert.Equal("*********A3B", response.Credentials.ClientSecretDisplayValue);
        Assert.Equal("*********Z9Y", response.Credentials.RefreshTokenDisplayValue);
        Assert.Equal("private", response.PrivacyStatus);
        Assert.False(response.SelfDeclaredMadeForKids);
        Assert.Null(response.SiteUrl);
        Assert.Null(response.ApplicationPasswordConfigured);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        Assert.Contains("*********A3B", json);
        Assert.Contains("*********Z9Y", json);
        Assert.DoesNotContain("stored-client-secret-A3B", json);
        Assert.DoesNotContain("stored-refresh-token-Z9Y", json);
    }

    [Fact]
    public void ToPublishSettingsResponse_WordPressSettings_ReturnsPasswordDisplayValue()
    {
        var response = PublishSettingsMapper.ToResponse(
            WordPressSettings(applicationPassword: "local-test-password"));

        AssertWordPressRedacted(response, rawApplicationPassword: "local-test-password");
    }

    [Fact]
    public void TypeOf_WordPressSettings_ReturnsWordPress()
    {
        var type = PublishSettingsMapper.TypeOf(WordPressSettings());

        Assert.Equal(PlatformType.WordPress, type);
    }
}

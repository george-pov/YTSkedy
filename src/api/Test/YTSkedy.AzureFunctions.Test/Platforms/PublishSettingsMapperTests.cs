using System.Text.Json;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using static YTSkedy.AzureFunctions.Test.Platforms.PlatformTestData;
using DomainWordPressSettings = YTSkedy.Scheduling.Domain.Platforms.WordPressSettings;

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
                refreshToken: "stored-refresh-token-Z9Y",
                categoryId: "27",
                containsSyntheticMedia: true));

        Assert.NotNull(response.Credentials);
        Assert.Equal(SchedulingSampleIds.YouTubeClientId, response.Credentials.ClientId);
        Assert.True(response.Credentials.ClientSecretConfigured);
        Assert.True(response.Credentials.RefreshTokenConfigured);
        Assert.Equal("*********A3B", response.Credentials.ClientSecretDisplayValue);
        Assert.Equal("*********Z9Y", response.Credentials.RefreshTokenDisplayValue);
        Assert.Equal("private", response.PrivacyStatus);
        Assert.False(response.SelfDeclaredMadeForKids);
        Assert.Equal("27", response.CategoryId);
        Assert.True(response.ContainsSyntheticMedia);
        Assert.Null(response.SiteUrl);
        Assert.Null(response.CategoryIds);
        Assert.Null(response.ApplicationPasswordConfigured);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("27", document.RootElement.GetProperty("categoryId").GetString());
        Assert.True(document.RootElement.GetProperty("containsSyntheticMedia").GetBoolean());
        Assert.Contains("*********A3B", json);
        Assert.Contains("*********Z9Y", json);
        Assert.DoesNotContain("stored-client-secret-A3B", json);
        Assert.DoesNotContain("stored-refresh-token-Z9Y", json);
    }

    [Fact]
    public void ToPublishSettingsResponse_YouTubeDefaults_IncludesExplicitDefaults()
    {
        var response = PublishSettingsMapper.ToResponse(YouTubeSettings());

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Null, root.GetProperty("categoryId").ValueKind);
        Assert.False(root.GetProperty("containsSyntheticMedia").GetBoolean());
    }

    [Fact]
    public void ToPublishSettingsResponse_WordPressSettings_ReturnsPasswordDisplayValue()
    {
        var response = PublishSettingsMapper.ToResponse(
            WordPressSettings(applicationPassword: "local-test-password"));

        AssertWordPressRedacted(response, rawApplicationPassword: "local-test-password");
    }

    [Fact]
    public void ToPublishSettingsResponse_WordPressSettings_ReturnsCopiedCategoryIds()
    {
        long[] categoryIds = [34, 12];
        var settings = WordPressSettings(categoryIds: categoryIds);

        var response = PublishSettingsMapper.ToResponse(settings);
        categoryIds[0] = 99;

        AssertWordPressRedacted(response, categoryIds: [34, 12]);
        Assert.NotSame(settings.CategoryIds, response.CategoryIds);
    }

    [Fact]
    public void ToPublishSettingsResponse_ScheduledWordPressSettings_ReturnsStickyAndScheduleOffset()
    {
        var response = PublishSettingsMapper.ToResponse(
            WordPressSettings(
                applicationPassword: "local-test-password",
                postStatus: DomainWordPressSettings.ScheduledPostStatus,
                sticky: true,
                scheduleOffsetHours: 25));

        Assert.Equal(DomainWordPressSettings.ScheduledPostStatus, response.PostStatus);
        Assert.True(response.Sticky);
        Assert.Equal(25, response.ScheduleOffsetHours);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.GetProperty("sticky").GetBoolean());
        Assert.Equal(25, root.GetProperty("scheduleOffsetHours").GetInt32());
        Assert.False(root.TryGetProperty("applicationPassword", out _));
        Assert.False(root.TryGetProperty("containsSyntheticMedia", out _));
        Assert.False(root.TryGetProperty("categoryId", out _));
        Assert.DoesNotContain("local-test-password", json);
    }

    [Fact]
    public void TypeOf_WordPressSettings_ReturnsWordPress()
    {
        var type = PublishSettingsMapper.TypeOf(WordPressSettings());

        Assert.Equal(PlatformType.WordPress, type);
    }
}

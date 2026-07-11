using System.Text.Json;
using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.Platforms;

public class PublishSettingsSerializerTests
{
    [Fact]
    public void Serialize_YouTubeSettings_RoundTrips()
    {
        var json = PublishSettingsSerializer.Serialize(
            PlatformType.YouTube,
            new YouTubeSettings(Credentials(), "unlisted", true));

        var settings = Assert.IsType<YouTubeSettings>(
            PublishSettingsSerializer.Deserialize(PlatformType.YouTube, json));
        Assert.Equal("client-id", settings.Credentials.ClientId);
        Assert.Equal("client-secret", settings.Credentials.ClientSecret);
        Assert.Equal("refresh-token", settings.Credentials.RefreshToken);
        Assert.Equal("unlisted", settings.PrivacyStatus);
        Assert.True(settings.SelfDeclaredMadeForKids);
    }

    [Fact]
    public void Serialize_WordPressSettings_RoundTripsSecretBearingSettings()
    {
        var json = PublishSettingsSerializer.Serialize(
            PlatformType.WordPress,
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                WordPressSettings.ScheduledPostStatus,
                [12, 34],
                sticky: true,
                scheduleOffsetHours: 25));

        var settings = Assert.IsType<WordPressSettings>(
            PublishSettingsSerializer.Deserialize(PlatformType.WordPress, json));
        Assert.Equal("https://example.com", settings.SiteUrl);
        Assert.Equal("editor", settings.Username);
        Assert.Equal("application-password", settings.ApplicationPassword);
        Assert.Equal(WordPressSettings.ScheduledPostStatus, settings.PostStatus);
        Assert.Equal([12, 34], settings.CategoryIds);
        Assert.True(settings.Sticky);
        Assert.Equal(25, settings.ScheduleOffsetHours);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(
            [12, 34],
            document.RootElement.GetProperty("categoryIds")
                .EnumerateArray()
                .Select(item => item.GetInt64()));
    }

    [Fact]
    public void Deserialize_WordPressLegacySettingsJson_ThrowsInvalidOperationException()
    {
        const string json = """
            {
              "siteUrl": "https://example.com",
              "username": "editor",
              "applicationPassword": "application-password",
              "postStatus": "publish"
            }
            """;

        Assert.Throws<InvalidOperationException>(
            () => PublishSettingsSerializer.Deserialize(PlatformType.WordPress, json));
    }

    [Fact]
    public void Deserialize_WordPressMalformedJson_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => PublishSettingsSerializer.Deserialize(
                PlatformType.WordPress,
                "{not-json"));
    }

    [Fact]
    public void Deserialize_WordPressInvalidSettings_ThrowsInvalidOperationException()
    {
        const string json = """
            {
              "siteUrl": "http://example.com",
              "username": "editor",
              "applicationPassword": "application-password",
              "postStatus": "publish"
            }
            """;

        Assert.Throws<InvalidOperationException>(
            () => PublishSettingsSerializer.Deserialize(PlatformType.WordPress, json));
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("https://user:password@example.com")]
    public void Deserialize_WordPressInvalidSiteUrl_ThrowsInvalidOperationException(
        string siteUrl)
    {
        var json = $$"""
            {
              "siteUrl": "{{siteUrl}}",
              "username": "editor",
              "applicationPassword": "application-password",
              "postStatus": "publish"
            }
            """;

        Assert.Throws<InvalidOperationException>(
            () => PublishSettingsSerializer.Deserialize(PlatformType.WordPress, json));
    }

    [Fact]
    public void SerializeSnapshot_YouTubeSettings_OmitsClientSecretAndRefreshToken()
    {
        var json = PublishSettingsSerializer.SerializeSnapshot(
            PlatformType.YouTube,
            new YouTubeSettings(Credentials(), "private", false));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var credentials = root.GetProperty("credentials");
        Assert.Equal("client-id", credentials.GetProperty("clientId").GetString());
        Assert.True(credentials.GetProperty("clientSecretConfigured").GetBoolean());
        Assert.True(credentials.GetProperty("refreshTokenConfigured").GetBoolean());
        Assert.Equal("private", root.GetProperty("privacyStatus").GetString());
        Assert.False(root.GetProperty("selfDeclaredMadeForKids").GetBoolean());
        Assert.False(credentials.TryGetProperty("clientSecret", out _));
        Assert.False(credentials.TryGetProperty("refreshToken", out _));
        Assert.DoesNotContain("client-secret", json);
        Assert.DoesNotContain("refresh-token", json);
    }

    [Fact]
    public void DeserializeSnapshot_YouTubeSnapshot_ReturnsTargetSnapshot()
    {
        var json = PublishSettingsSerializer.SerializeSnapshot(
            PlatformType.YouTube,
            new YouTubeSettings(Credentials(), "private", false));

        var snapshot = PublishSettingsSerializer.DeserializeSnapshot(PlatformType.YouTube, json);

        Assert.NotNull(snapshot);
        Assert.Equal(PlatformType.YouTube, snapshot!.PlatformType);
        Assert.Equal("client-id", snapshot.YouTubeClientId);
        Assert.Null(snapshot.WordPressSiteUrl);
    }

    [Fact]
    public void SerializeSnapshot_WordPressSettings_OmitsApplicationPassword()
    {
        var json = PublishSettingsSerializer.SerializeSnapshot(
            PlatformType.WordPress,
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                WordPressSettings.ScheduledPostStatus,
                [12, 34],
                sticky: true,
                scheduleOffsetHours: 25));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("https://example.com", root.GetProperty("siteUrl").GetString());
        Assert.False(root.TryGetProperty("username", out _));
        Assert.False(root.TryGetProperty("postStatus", out _));
        Assert.False(root.TryGetProperty("sticky", out _));
        Assert.False(root.TryGetProperty("scheduleOffsetHours", out _));
        Assert.False(root.TryGetProperty("categoryIds", out _));
        Assert.False(root.TryGetProperty("applicationPassword", out _));
        Assert.False(root.TryGetProperty("passwordDisplayValue", out _));
        Assert.DoesNotContain("application-password", json);
    }

    [Fact]
    public void DeserializeSnapshot_WordPressSnapshot_ReturnsTargetSnapshot()
    {
        var json = PublishSettingsSerializer.SerializeSnapshot(
            PlatformType.WordPress,
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "draft",
                [12, 34]));

        var snapshot = PublishSettingsSerializer.DeserializeSnapshot(PlatformType.WordPress, json);

        Assert.NotNull(snapshot);
        Assert.Equal(PlatformType.WordPress, snapshot!.PlatformType);
        Assert.Equal("https://example.com", snapshot.WordPressSiteUrl);
        Assert.Null(snapshot.YouTubeClientId);
    }

    [Fact]
    public void DeserializeSnapshot_MalformedJson_ReturnsNull()
    {
        Assert.Null(PublishSettingsSerializer.DeserializeSnapshot(PlatformType.YouTube, "{not-json"));
    }

    private static YouTubeCredentials Credentials() =>
        new("client-id", "client-secret", "refresh-token");
}

using System.Text.Json;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;

namespace YTSkedy.AzureFunctions.Test.Platforms;

internal static class PlatformTestData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static PublishSettingsPayload YouTubePayload(
        string? clientId = SchedulingSampleIds.YouTubeClientId,
        string? clientSecret = SchedulingSampleIds.YouTubeClientSecret,
        string? refreshToken = SchedulingSampleIds.YouTubeRefreshToken,
        string? privacyStatus = "private",
        bool? selfDeclaredMadeForKids = false) =>
        new(
            new YouTubeCredentialsPayload(clientId, clientSecret, refreshToken),
            privacyStatus,
            selfDeclaredMadeForKids,
            null,
            null,
            null,
            null);

    public static PublishSettingsPayload WordPressPayload(
        string? siteUrl = "https://example.com",
        string? username = "editor",
        string? applicationPassword = null,
        string? postStatus = "publish") =>
        new(
            null,
            null,
            null,
            siteUrl,
            username,
            applicationPassword,
            postStatus);

    public static PublishingContentPayload PublishingPayload(
        string? titleTemplateId = SchedulingSampleIds.TitleTemplateId,
        string? descriptionTemplateId = SchedulingSampleIds.DescriptionTemplateId) =>
        new(titleTemplateId, descriptionTemplateId);

    public static PublishingContent RequiredPublishingContent(
        string titleTemplateId = SchedulingSampleIds.TitleTemplateId,
        string descriptionTemplateId = SchedulingSampleIds.DescriptionTemplateId) =>
        SchedulingSamples.PublishingContent(titleTemplateId, descriptionTemplateId);

    public static PlatformView WordPressPlatform() =>
        new(
            "wp-platform",
            "Main WordPress site",
            "company-blog",
            PlatformType.WordPress,
            WordPressSettings(applicationPassword: "stored-password"),
            RequiredPublishingContent());

    public static YouTubeSettings YouTubeSettings(
        string clientId = SchedulingSampleIds.YouTubeClientId,
        string clientSecret = SchedulingSampleIds.YouTubeClientSecret,
        string refreshToken = SchedulingSampleIds.YouTubeRefreshToken,
        string privacyStatus = "private") =>
        new(new YouTubeCredentials(clientId, clientSecret, refreshToken), privacyStatus, false);

    public static WordPressSettings WordPressSettings(
        string applicationPassword = "application-password",
        string postStatus = "publish") =>
        new("https://example.com", "editor", applicationPassword, postStatus);

    public static CreatePlatformCommand WordPressCreateCommand(
        string? referenceKey = "company-blog",
        PublishingContent? publishingContent = null) =>
        new(
            "Main WordPress site",
            PlatformType.WordPress,
            WordPressSettings(),
            publishingContent ?? RequiredPublishingContent(),
            referenceKey);

    public static UpdatePlatformCommand WordPressUpdateCommand(
        string platformId = "wp-platform",
        string? referenceKey = "company-blog",
        PublishingContent? publishingContent = null,
        string postStatus = "draft") =>
        new(
            platformId,
            "Main WordPress site",
            referenceKey,
            WordPressSettings(postStatus: postStatus),
            publishingContent ?? RequiredPublishingContent());

    public static void AssertWordPressRedacted(
        PublishSettingsResponse response,
        string postStatus = "publish",
        string rawApplicationPassword = "application-password")
    {
        Assert.Null(response.Credentials);
        Assert.Null(response.PrivacyStatus);
        Assert.Null(response.SelfDeclaredMadeForKids);
        Assert.Equal("https://example.com", response.SiteUrl);
        Assert.Equal("editor", response.Username);
        Assert.Equal(postStatus, response.PostStatus);
        Assert.True(response.ApplicationPasswordConfigured);
        Assert.Equal("*******", response.PasswordDisplayValue);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(
            "*******",
            document.RootElement.GetProperty("passwordDisplayValue").GetString());
        Assert.False(document.RootElement.TryGetProperty("applicationPassword", out _));
        Assert.DoesNotContain(rawApplicationPassword, json);
    }
}

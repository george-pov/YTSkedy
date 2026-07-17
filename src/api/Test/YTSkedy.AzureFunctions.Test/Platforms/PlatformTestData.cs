using System.Text.Json;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;

namespace YTSkedy.AzureFunctions.Test.Platforms;

internal static class PlatformTestData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static PublishSettingsRequest CreateYouTubeSettingsRequest(
        string? clientId = SchedulingSampleIds.YouTubeClientId,
        string? clientSecret = SchedulingSampleIds.YouTubeClientSecret,
        string? refreshToken = SchedulingSampleIds.YouTubeRefreshToken,
        string? privacyStatus = "private",
        bool? selfDeclaredMadeForKids = false,
        string? categoryId = null,
        bool? containsSyntheticMedia = null) =>
        new(
            new YouTubeCredentialsRequest(clientId, clientSecret, refreshToken),
            privacyStatus,
            selfDeclaredMadeForKids,
            null,
            null,
            null,
            null,
            CategoryId: categoryId,
            ContainsSyntheticMedia: containsSyntheticMedia);

    public static PublishSettingsRequest CreateWordPressSettingsRequest(
        string? siteUrl = "https://example.com",
        string? username = "editor",
        string? applicationPassword = null,
        string? postStatus = "publish",
        bool? sticky = null,
        int? scheduleOffsetHours = null,
        IReadOnlyList<long>? categoryIds = null,
        bool useNullCategoryIds = false) =>
        new(
            null,
            null,
            null,
            siteUrl,
            username,
            applicationPassword,
            postStatus,
            sticky,
            scheduleOffsetHours,
            useNullCategoryIds ? null : categoryIds ?? []);

    public static PublishingContentRequest CreatePublishingContentRequest(
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
        string privacyStatus = "private",
        string? categoryId = null,
        bool containsSyntheticMedia = false) =>
        new(
            new YouTubeCredentials(clientId, clientSecret, refreshToken),
            privacyStatus,
            false,
            categoryId,
            containsSyntheticMedia);

    public static WordPressSettings WordPressSettings(
        string applicationPassword = "application-password",
        string postStatus = "publish",
        IReadOnlyList<long>? categoryIds = null,
        bool sticky = false,
        int? scheduleOffsetHours = null) =>
        new(
            "https://example.com",
            "editor",
            applicationPassword,
            postStatus,
            categoryIds ?? [],
            sticky,
            scheduleOffsetHours);

    public static CreatePlatformCommand WordPressCreateCommand(
        string? referenceKey = "company-blog",
        PublishingContent? publishingContent = null,
        IReadOnlyList<long>? categoryIds = null) =>
        new(
            "Main WordPress site",
            PlatformType.WordPress,
            WordPressSettings(categoryIds: categoryIds),
            publishingContent ?? RequiredPublishingContent(),
            referenceKey);

    public static UpdatePlatformCommand WordPressUpdateCommand(
        string platformId = "wp-platform",
        string? referenceKey = "company-blog",
        PublishingContent? publishingContent = null,
        string postStatus = "draft",
        IReadOnlyList<long>? categoryIds = null) =>
        new(
            platformId,
            "Main WordPress site",
            referenceKey,
            WordPressSettings(postStatus: postStatus, categoryIds: categoryIds),
            publishingContent ?? RequiredPublishingContent());

    public static void AssertWordPressRedacted(
        PublishSettingsResponse response,
        string postStatus = "publish",
        string rawApplicationPassword = "application-password",
        IReadOnlyList<long>? categoryIds = null)
    {
        Assert.Null(response.Credentials);
        Assert.Null(response.PrivacyStatus);
        Assert.Null(response.SelfDeclaredMadeForKids);
        Assert.Equal("https://example.com", response.SiteUrl);
        Assert.Equal("editor", response.Username);
        Assert.Equal(postStatus, response.PostStatus);
        Assert.False(response.Sticky);
        Assert.Null(response.ScheduleOffsetHours);
        Assert.Equal(categoryIds ?? [], response.CategoryIds);
        Assert.True(response.ApplicationPasswordConfigured);
        Assert.Equal("*******", response.PasswordDisplayValue);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(
            "*******",
            document.RootElement.GetProperty("passwordDisplayValue").GetString());
        Assert.False(document.RootElement.TryGetProperty("applicationPassword", out _));
        Assert.Equal(
            categoryIds ?? [],
            document.RootElement.GetProperty("categoryIds")
                .EnumerateArray()
                .Select(item => item.GetInt64()));
        Assert.DoesNotContain(rawApplicationPassword, json);
    }
}

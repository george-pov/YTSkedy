using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Test.Platforms;

public sealed class PlatformsApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("YouTube", PlatformType.YouTube)]
    [InlineData("youtube", PlatformType.YouTube)]
    [InlineData("WordPress", PlatformType.WordPress)]
    [InlineData("wordpress", PlatformType.WordPress)]
    public void TryParsePlatformType_KnownType_ReturnsTrue(string value, PlatformType expected)
    {
        var parsed = PlatformsApi.TryParsePlatformType(value, out var type);

        Assert.True(parsed);
        Assert.Equal(expected, type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bogus")]
    [InlineData("0")]
    public void TryParsePlatformType_UnknownType_ReturnsFalse(string? value)
    {
        var parsed = PlatformsApi.TryParsePlatformType(value, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryBuildCreateCommand_YouTubeValidRequest_BuildsCommand()
    {
        var request = new CreatePlatformRequest(
            "Main YouTube channel",
            "YouTube",
            null,
            YouTubePayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Equal("Main YouTube channel", command.Name);
        Assert.Equal(PlatformType.YouTube, command.Type);
        var settings = Assert.IsType<YouTubeSettings>(command.PublishSettings);
        Assert.Equal("client-id", settings.Credentials.ClientId);
        Assert.Equal("client-secret", settings.Credentials.ClientSecret);
        Assert.Equal("refresh-token", settings.Credentials.RefreshToken);
        Assert.Equal("private", settings.PrivacyStatus);
        Assert.False(settings.SelfDeclaredMadeForKids);
    }

    [Fact]
    public void TryBuildCreateCommand_YouTubeMissingClientSecret_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main YouTube channel",
            "YouTube",
            null,
            YouTubePayload(clientSecret: ""));

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings credentials client secret is required.",
            BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressValidRequest_BuildsCommand()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(applicationPassword: "application-password"));

        var built = PlatformsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Equal("Main WordPress site", command.Name);
        Assert.Equal(PlatformType.WordPress, command.Type);
        var settings = Assert.IsType<WordPressSettings>(command.PublishSettings);
        Assert.Equal("https://example.com", settings.SiteUrl);
        Assert.Equal("editor", settings.Username);
        Assert.Equal("application-password", settings.ApplicationPassword);
        Assert.Equal("publish", settings.PostStatus);
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressMissingSiteUrl_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(siteUrl: ""));

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal("Publish settings site URL is required.", BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressInsecureRemoteSiteUrl_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(siteUrl: "http://example.com"));

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings site URL must use HTTPS unless it targets localhost or 127.0.0.1.",
            BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressCredentialBearingUrl_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(siteUrl: "https://user:password@example.com"));

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        var message = BadRequestMessage(error);
        Assert.Equal(
            "Publish settings site URL must be an absolute HTTP(S) URL without credentials.",
            message);
        Assert.DoesNotContain("user:password", message);
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressMissingApplicationPassword_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(applicationPassword: ""));

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings Application Password is required.",
            BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressInvalidPostStatus_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(applicationPassword: "application-password", postStatus: "pending"));

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings post status must be 'publish' or 'draft'.",
            BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_ValidReferenceKey_PreservesDisplayCasing()
    {
        var request = new CreatePlatformRequest(
            "Main YouTube channel",
            "YouTube",
            "youTube1",
            YouTubePayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Equal("youTube1", command.ReferenceKey);
    }

    [Fact]
    public void TryBuildCreateCommand_BlankReferenceKey_SetsNull()
    {
        var request = new CreatePlatformRequest(
            "Main YouTube channel",
            "YouTube",
            "   ",
            YouTubePayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Null(command.ReferenceKey);
    }

    [Fact]
    public void TryBuildCreateCommand_InvalidReferenceKey_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main YouTube channel",
            "YouTube",
            "bad_key",
            YouTubePayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Reference key must be 1 to 15 characters and contain only letters, numbers, or hyphen.",
            BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildUpdateCommand_WordPressBlankApplicationPassword_PreservesExisting()
    {
        var existing = WordPressPlatform();
        var request = new UpdatePlatformRequest(
            "Renamed WordPress site",
            null,
            WordPressPayload(applicationPassword: "   ", postStatus: "draft"));

        var built = PlatformsApi.TryBuildUpdateCommand(existing, request, out var command, out _);

        Assert.True(built);
        Assert.Equal("wp-platform", command.PlatformId);
        Assert.Equal("Renamed WordPress site", command.Name);
        Assert.Null(command.ReferenceKey);
        var settings = Assert.IsType<WordPressSettings>(command.PublishSettings);
        Assert.Equal("stored-password", settings.ApplicationPassword);
        Assert.Equal("draft", settings.PostStatus);
    }

    [Fact]
    public void TryBuildUpdateCommand_WordPressReplacementApplicationPassword_ReplacesExisting()
    {
        var existing = WordPressPlatform();
        var request = new UpdatePlatformRequest(
            "Renamed WordPress site",
            null,
            WordPressPayload(applicationPassword: "replacement-password"));

        var built = PlatformsApi.TryBuildUpdateCommand(existing, request, out var command, out _);

        Assert.True(built);
        var settings = Assert.IsType<WordPressSettings>(command.PublishSettings);
        Assert.Equal("replacement-password", settings.ApplicationPassword);
    }

    [Fact]
    public void TryBuildUpdateCommand_YouTubeValidRequest_BuildsCommand()
    {
        var existing = new PlatformView(
            "yt-platform",
            "Main YouTube channel",
            null,
            PlatformType.YouTube,
            YouTubeSettings(
                clientId: "old-client-id",
                clientSecret: "stored-client-secret",
                refreshToken: "stored-refresh-token"));
        var request = new UpdatePlatformRequest(
            "Renamed YouTube channel",
            null,
            YouTubePayload(
                clientId: "new-client-id",
                clientSecret: "",
                refreshToken: null,
                privacyStatus: "unlisted"));

        var built = PlatformsApi.TryBuildUpdateCommand(existing, request, out var command, out _);

        Assert.True(built);
        Assert.Equal("yt-platform", command.PlatformId);
        var settings = Assert.IsType<YouTubeSettings>(command.PublishSettings);
        Assert.Equal("new-client-id", settings.Credentials.ClientId);
        Assert.Equal("stored-client-secret", settings.Credentials.ClientSecret);
        Assert.Equal("stored-refresh-token", settings.Credentials.RefreshToken);
        Assert.Equal("unlisted", settings.PrivacyStatus);
    }

    [Fact]
    public void TryBuildUpdateCommand_ValidReferenceKey_BuildsCommand()
    {
        var existing = WordPressPlatform();
        var request = new UpdatePlatformRequest(
            "Renamed WordPress site",
            "blog-1",
            WordPressPayload(applicationPassword: "replacement-password"));

        var built = PlatformsApi.TryBuildUpdateCommand(existing, request, out var command, out _);

        Assert.True(built);
        Assert.Equal("blog-1", command.ReferenceKey);
    }

    [Fact]
    public void ToCreateResult_WordPressCreated_ReturnsRedactedResponse()
    {
        var command = new CreatePlatformCommand(
            "Main WordPress site",
            PlatformType.WordPress,
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "publish"),
            "company-blog");

        var actionResult = PlatformsApi.ToCreateResult(
            CreatePlatformResult.Created("wp-platform"),
            command);

        var response = AssertPlatformOk(actionResult);
        Assert.Equal("wp-platform", response.PlatformId);
        Assert.Equal("company-blog", response.ReferenceKey);
        Assert.Equal("WordPress", response.Type);
        AssertWordPressRedacted(response.PublishSettings);
    }

    [Fact]
    public void ToCreateResult_DuplicateReferenceKey_Returns409()
    {
        var command = new CreatePlatformCommand(
            "Main WordPress site",
            PlatformType.WordPress,
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "publish"),
            "company-blog");

        var actionResult = PlatformsApi.ToCreateResult(
            CreatePlatformResult.ReferenceKeyAlreadyExists(),
            command);

        var conflict = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal("A platform reference key 'company-blog' already exists.", conflict.Value);
    }

    [Fact]
    public void ToUpdateResult_WordPressUpdated_ReturnsRedactedResponse()
    {
        var command = new UpdatePlatformCommand(
            "wp-platform",
            "Main WordPress site",
            "company-blog",
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "draft"));

        var actionResult = PlatformsApi.ToUpdateResult(UpdatePlatformResult.Updated, command);

        var response = AssertPlatformOk(actionResult);
        Assert.Equal("company-blog", response.ReferenceKey);
        Assert.Equal("WordPress", response.Type);
        AssertWordPressRedacted(response.PublishSettings, "draft");
    }

    [Fact]
    public void ToUpdateResult_DuplicateReferenceKey_Returns409()
    {
        var command = new UpdatePlatformCommand(
            "wp-platform",
            "Main WordPress site",
            "company-blog",
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "draft"));

        var actionResult = PlatformsApi.ToUpdateResult(
            UpdatePlatformResult.ReferenceKeyAlreadyExists,
            command);

        var conflict = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal("A platform reference key 'company-blog' already exists.", conflict.Value);
    }

    [Fact]
    public void ToUpdateResult_NotFound_Returns404()
    {
        var command = new UpdatePlatformCommand(
            "missing",
            "Main WordPress site",
            null,
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "draft"));

        var actionResult = PlatformsApi.ToUpdateResult(UpdatePlatformResult.NotFound, command);

        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public void PlatformResponse_WithReferenceKey_SerializesCamelCaseAndRedactsSecrets()
    {
        var response = PlatformsApi.ToPlatformResponse(
            "wp-platform",
            "Main WordPress site",
            PlatformType.WordPress,
            "company-blog",
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "draft"));

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("company-blog", document.RootElement.GetProperty("referenceKey").GetString());
        Assert.Equal(
            "https://example.com",
            document.RootElement.GetProperty("publishSettings").GetProperty("siteUrl").GetString());
        Assert.DoesNotContain("applicationPassword\":\"", json);
        Assert.DoesNotContain("application-password", json);
    }

    [Fact]
    public void ToPublishSettingsResponse_YouTubeSettings_RedactsSecrets()
    {
        var response = PlatformPublishSettingsHttpMapper.ToResponse(
            YouTubeSettings());

        Assert.NotNull(response.Credentials);
        Assert.Equal("client-id", response.Credentials.ClientId);
        Assert.True(response.Credentials.ClientSecretConfigured);
        Assert.True(response.Credentials.RefreshTokenConfigured);
        Assert.Equal("private", response.PrivacyStatus);
        Assert.False(response.SelfDeclaredMadeForKids);
        Assert.Null(response.SiteUrl);
        Assert.Null(response.ApplicationPasswordConfigured);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        Assert.DoesNotContain("client-secret", json);
        Assert.DoesNotContain("refresh-token", json);
    }

    [Fact]
    public void ToPublishSettingsResponse_WordPressSettings_RedactsApplicationPassword()
    {
        var response = PlatformPublishSettingsHttpMapper.ToResponse(
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "publish"));

        AssertWordPressRedacted(response);
    }

    [Fact]
    public void TypeOf_WordPressSettings_ReturnsWordPress()
    {
        var type = PlatformPublishSettingsHttpMapper.TypeOf(
            new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "publish"));

        Assert.Equal(PlatformType.WordPress, type);
    }

    private static PublishSettingsPayload YouTubePayload(
        string? clientId = "client-id",
        string? clientSecret = "client-secret",
        string? refreshToken = "refresh-token",
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

    private static PublishSettingsPayload WordPressPayload(
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

    private static PlatformView WordPressPlatform() =>
        new(
            "wp-platform",
            "Main WordPress site",
            "company-blog",
            PlatformType.WordPress,
            new WordPressSettings(
                "https://example.com",
                "editor",
                "stored-password",
                "publish"));

    private static YouTubeSettings YouTubeSettings(
        string clientId = "client-id",
        string clientSecret = "client-secret",
        string refreshToken = "refresh-token") =>
        new(new YouTubeCredentials(clientId, clientSecret, refreshToken), "private", false);

    private static string BadRequestMessage(IActionResult actionResult)
    {
        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        return Assert.IsType<string>(badRequest.Value);
    }

    private static PlatformResponse AssertPlatformOk(IActionResult actionResult)
    {
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        return Assert.IsType<PlatformResponse>(ok.Value);
    }

    private static void AssertWordPressRedacted(
        PublishSettingsResponse response,
        string postStatus = "publish")
    {
        Assert.Null(response.Credentials);
        Assert.Null(response.PrivacyStatus);
        Assert.Null(response.SelfDeclaredMadeForKids);
        Assert.Equal("https://example.com", response.SiteUrl);
        Assert.Equal("editor", response.Username);
        Assert.Equal(postStatus, response.PostStatus);
        Assert.True(response.ApplicationPasswordConfigured);

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("applicationPassword", out _));
        Assert.DoesNotContain("application-password", json);
    }
}

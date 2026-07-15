using YTSkedy.AzureFunctions.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;
using static YTSkedy.AzureFunctions.Test.Platforms.PlatformTestData;
using DomainWordPressSettings = YTSkedy.Scheduling.Domain.Platforms.WordPressSettings;

namespace YTSkedy.AzureFunctions.Test.Platforms;

public sealed class PlatformsApiCommandTests
{
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
            YouTubePayload(categoryId: "27", containsSyntheticMedia: true),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Equal("Main YouTube channel", command.Name);
        Assert.Equal(PlatformType.YouTube, command.Type);
        var settings = Assert.IsType<YouTubeSettings>(command.PublishSettings);
        Assert.Equal(SchedulingSampleIds.YouTubeClientId, settings.Credentials.ClientId);
        Assert.Equal(SchedulingSampleIds.YouTubeClientSecret, settings.Credentials.ClientSecret);
        Assert.Equal(SchedulingSampleIds.YouTubeRefreshToken, settings.Credentials.RefreshToken);
        Assert.Equal("private", settings.PrivacyStatus);
        Assert.False(settings.SelfDeclaredMadeForKids);
        Assert.Equal("27", settings.CategoryId);
        Assert.True(settings.ContainsSyntheticMedia);
        Assert.Equal(SchedulingSampleIds.TitleTemplateId, command.PublishingContent.TitleTemplateId);
        Assert.Equal(
            SchedulingSampleIds.DescriptionTemplateId,
            command.PublishingContent.DescriptionTemplateId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryBuildCreateCommand_YouTubeBlankCategoryId_ReturnsBadRequest(
        string categoryId)
    {
        var request = new CreatePlatformRequest(
            "Main YouTube channel",
            "YouTube",
            null,
            YouTubePayload(categoryId: categoryId));

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings categoryId must be omitted, null, or a non-blank YouTube category ID.",
            ActionResultAssertions.BadRequestMessage(error));
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
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressValidRequest_BuildsCommand()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(applicationPassword: "application-password"),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Equal("Main WordPress site", command.Name);
        Assert.Equal(PlatformType.WordPress, command.Type);
        var settings = Assert.IsType<WordPressSettings>(command.PublishSettings);
        Assert.Equal("https://example.com", settings.SiteUrl);
        Assert.Equal("editor", settings.Username);
        Assert.Equal("application-password", settings.ApplicationPassword);
        Assert.Empty(settings.CategoryIds);
        Assert.Equal("publish", settings.PostStatus);
        Assert.False(settings.Sticky);
        Assert.Null(settings.ScheduleOffsetHours);
        Assert.Equal(SchedulingSampleIds.TitleTemplateId, command.PublishingContent.TitleTemplateId);
        Assert.Equal(
            SchedulingSampleIds.DescriptionTemplateId,
            command.PublishingContent.DescriptionTemplateId);
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressCategoryIds_BuildsCommandInOrder()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(
                applicationPassword: "application-password",
                categoryIds: [34, 12]),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Equal(
            [34, 12],
            Assert.IsType<WordPressSettings>(command.PublishSettings).CategoryIds);
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressMissingCategoryIds_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(
                applicationPassword: "application-password",
                useNullCategoryIds: true),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings categoryIds are required.",
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Theory]
    [MemberData(nameof(InvalidCategoryIds))]
    public void TryBuildCreateCommand_WordPressInvalidCategoryIds_ReturnsBadRequest(
        IReadOnlyList<long> categoryIds)
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(
                applicationPassword: "application-password",
                categoryIds: categoryIds),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings categoryIds must contain distinct positive integers.",
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildUpdateCommand_WordPressCategoryIds_ReplacesSelection()
    {
        var request = new UpdatePlatformRequest(
            "Renamed WordPress site",
            null,
            WordPressPayload(
                applicationPassword: "   ",
                categoryIds: [34, 12]),
            PublishingPayload());

        var built = PlatformsApi.TryBuildUpdateCommand(
            WordPressPlatform(),
            request,
            out var command,
            out _);

        Assert.True(built);
        var settings = Assert.IsType<WordPressSettings>(command.PublishSettings);
        Assert.Equal([34, 12], settings.CategoryIds);
        Assert.Equal("stored-password", settings.ApplicationPassword);
    }

    public static TheoryData<IReadOnlyList<long>> InvalidCategoryIds =>
        new()
        {
            new long[] { 0 },
            new long[] { -1 },
            new long[] { 12, 12 }
        };

    [Theory]
    [InlineData("pending")]
    [InlineData("private")]
    public void TryBuildCreateCommand_WordPressAllowedNonScheduledPostStatus_BuildsCommand(
        string postStatus)
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(
                applicationPassword: "application-password",
                postStatus: postStatus),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        var settings = Assert.IsType<WordPressSettings>(command.PublishSettings);
        Assert.Equal(postStatus, settings.PostStatus);
        Assert.Null(settings.ScheduleOffsetHours);
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressFuturePostStatusWithOffset_BuildsCommand()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(
                applicationPassword: "application-password",
                postStatus: DomainWordPressSettings.ScheduledPostStatus,
                sticky: true,
                scheduleOffsetHours: 25),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        var settings = Assert.IsType<WordPressSettings>(command.PublishSettings);
        Assert.Equal(DomainWordPressSettings.ScheduledPostStatus, settings.PostStatus);
        Assert.True(settings.Sticky);
        Assert.Equal(25, settings.ScheduleOffsetHours);
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
        Assert.Equal(
            "Publish settings site URL is required.",
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_PublishingContentTemplateIds_BuildsCommand()
    {
        var request = new CreatePlatformRequest(
            "Main YouTube channel",
            "YouTube",
            null,
            YouTubePayload(),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out var command, out _);

        Assert.True(built);
        Assert.Equal(SchedulingSampleIds.TitleTemplateId, command.PublishingContent.TitleTemplateId);
        Assert.Equal(
            SchedulingSampleIds.DescriptionTemplateId,
            command.PublishingContent.DescriptionTemplateId);
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
            ActionResultAssertions.BadRequestMessage(error));
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
        var message = ActionResultAssertions.BadRequestMessage(error);
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
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressInvalidPostStatus_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(applicationPassword: "application-password", postStatus: "scheduled"));

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings post status must be 'draft', 'pending', 'private', 'future', or 'publish'.",
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressFuturePostStatusWithoutOffset_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(
                applicationPassword: "application-password",
                postStatus: DomainWordPressSettings.ScheduledPostStatus),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings schedule offset hours must be provided when post status is 'future'.",
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_WordPressNonFuturePostStatusWithOffset_ReturnsBadRequest()
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(
                applicationPassword: "application-password",
                postStatus: "draft",
                scheduleOffsetHours: 1),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings schedule offset hours must be omitted unless post status is 'future'.",
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(DomainWordPressSettings.MaxScheduleOffsetHours + 1)]
    public void TryBuildCreateCommand_WordPressFuturePostStatusWithInvalidOffset_ReturnsBadRequest(
        int scheduleOffsetHours)
    {
        var request = new CreatePlatformRequest(
            "Main WordPress site",
            "WordPress",
            null,
            WordPressPayload(
                applicationPassword: "application-password",
                postStatus: DomainWordPressSettings.ScheduledPostStatus,
                scheduleOffsetHours: scheduleOffsetHours),
            PublishingPayload());

        var built = PlatformsApi.TryBuildCreateCommand(request, out _, out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings schedule offset hours must be between 1 and 168.",
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildUpdateCommand_WordPressFuturePostStatusWithInvalidOffset_ReturnsBadRequest()
    {
        var request = new UpdatePlatformRequest(
            "Renamed WordPress site",
            null,
            WordPressPayload(
                applicationPassword: "   ",
                postStatus: DomainWordPressSettings.ScheduledPostStatus,
                scheduleOffsetHours: DomainWordPressSettings.MaxScheduleOffsetHours + 1),
            PublishingPayload());

        var built = PlatformsApi.TryBuildUpdateCommand(
            WordPressPlatform(),
            request,
            out _,
            out var error);

        Assert.False(built);
        Assert.Equal(
            "Publish settings schedule offset hours must be between 1 and 168.",
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildCreateCommand_ValidReferenceKey_PreservesDisplayCasing()
    {
        var request = new CreatePlatformRequest(
            "Main YouTube channel",
            "YouTube",
            "youTube1",
            YouTubePayload(),
            PublishingPayload());

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
            YouTubePayload(),
            PublishingPayload());

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
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildUpdateCommand_WordPressBlankApplicationPassword_PreservesExisting()
    {
        var request = new UpdatePlatformRequest(
            "Renamed WordPress site",
            null,
            WordPressPayload(applicationPassword: "   ", postStatus: "draft"),
            PublishingPayload());

        var built = PlatformsApi.TryBuildUpdateCommand(
            WordPressPlatform(),
            request,
            out var command,
            out _);

        Assert.True(built);
        Assert.Equal("wp-platform", command.PlatformId);
        Assert.Equal("Renamed WordPress site", command.Name);
        Assert.Null(command.ReferenceKey);
        var settings = Assert.IsType<WordPressSettings>(command.PublishSettings);
        Assert.Equal("stored-password", settings.ApplicationPassword);
        Assert.Equal("draft", settings.PostStatus);
        Assert.False(settings.Sticky);
        Assert.Null(settings.ScheduleOffsetHours);
        Assert.Equal(SchedulingSampleIds.TitleTemplateId, command.PublishingContent.TitleTemplateId);
        Assert.Equal(
            SchedulingSampleIds.DescriptionTemplateId,
            command.PublishingContent.DescriptionTemplateId);
    }

    [Fact]
    public void TryBuildUpdateCommand_WordPressReplacementApplicationPassword_ReplacesExisting()
    {
        var request = new UpdatePlatformRequest(
            "Renamed WordPress site",
            null,
            WordPressPayload(applicationPassword: "replacement-password"),
            PublishingPayload());

        var built = PlatformsApi.TryBuildUpdateCommand(
            WordPressPlatform(),
            request,
            out var command,
            out _);

        Assert.True(built);
        var settings = Assert.IsType<WordPressSettings>(command.PublishSettings);
        Assert.Equal("replacement-password", settings.ApplicationPassword);
    }

    [Fact]
    public void TryBuildUpdateCommand_YouTubeValidRequest_BuildsCommand()
    {
        var existing = new PlatformView(
            SchedulingSampleIds.YouTubePlatformId,
            "Main YouTube channel",
            null,
            PlatformType.YouTube,
            YouTubeSettings(
                clientId: "old-client-id",
                clientSecret: "stored-client-secret",
                refreshToken: "stored-refresh-token"),
            RequiredPublishingContent());
        var request = new UpdatePlatformRequest(
            "Renamed YouTube channel",
            null,
            YouTubePayload(
                clientId: "new-client-id",
                clientSecret: "",
                refreshToken: null,
                privacyStatus: "unlisted",
                categoryId: " 27 ",
                containsSyntheticMedia: true),
            PublishingPayload());

        var built = PlatformsApi.TryBuildUpdateCommand(existing, request, out var command, out _);

        Assert.True(built);
        Assert.Equal(SchedulingSampleIds.YouTubePlatformId, command.PlatformId);
        var settings = Assert.IsType<YouTubeSettings>(command.PublishSettings);
        Assert.Equal("new-client-id", settings.Credentials.ClientId);
        Assert.Equal("stored-client-secret", settings.Credentials.ClientSecret);
        Assert.Equal("stored-refresh-token", settings.Credentials.RefreshToken);
        Assert.Equal("unlisted", settings.PrivacyStatus);
        Assert.Equal("27", settings.CategoryId);
        Assert.True(settings.ContainsSyntheticMedia);
    }

    [Fact]
    public void TryBuildUpdateCommand_MissingPublishingContent_ReturnsBadRequest()
    {
        var request = new UpdatePlatformRequest(
            "Renamed WordPress site",
            null,
            WordPressPayload(applicationPassword: "replacement-password"));

        var built = PlatformsApi.TryBuildUpdateCommand(
            WordPressPlatform(),
            request,
            out _,
            out var error);

        Assert.False(built);
        Assert.Equal(
            "Publishing content titleTemplateId and descriptionTemplateId are required.",
            ActionResultAssertions.BadRequestMessage(error));
    }

    [Fact]
    public void TryBuildUpdateCommand_ValidReferenceKey_BuildsCommand()
    {
        var request = new UpdatePlatformRequest(
            "Renamed WordPress site",
            "blog-1",
            WordPressPayload(applicationPassword: "replacement-password"),
            PublishingPayload());

        var built = PlatformsApi.TryBuildUpdateCommand(
            WordPressPlatform(),
            request,
            out var command,
            out _);

        Assert.True(built);
        Assert.Equal("blog-1", command.ReferenceKey);
    }
}

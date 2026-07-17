using static YTSkedy.Scheduling.Application.Test.PublishHandlerScenario;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerContentTests
{
    private readonly PublishHandlerScenario _scenario = new();

    [Fact]
    public async Task HandleAsync_NoTitleText_ReturnsInvalidPublishingContent()
    {
        _scenario.CalendarEvent = Event(FutureStart, Text(title: null));

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt();
    }

    [Fact]
    public async Task HandleAsync_BlankTitleText_ReturnsInvalidPublishingContent()
    {
        _scenario.CalendarEvent = Event(
            FutureStart,
            Text(title: "   ", description: "description"));

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt();
    }

    [Fact]
    public async Task HandleAsync_TemplateContent_RendersBeforePublishing()
    {
        _scenario.SelectedPlatform = Platform(
            new PublishingContent("title-template", "description-template"));
        _scenario.ActivePlatforms = [_scenario.SelectedPlatform];
        SetTemplates(
            new TemplateView(
                "title-template",
                "Title template",
                TemplateType.YouTube,
                "{{ text1 }} on {{ shortDateEn }}"),
            new TemplateView(
                "description-template",
                "Description template",
                TemplateType.YouTube,
                "Details: {{ text2 }}"));

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        _scenario.Publisher.Verify(candidate => candidate.PublishAsync(
            It.Is<PlatformPublishRequest>(request =>
                request.Title == "English title on 2026-06-25" &&
                request.Description == "Details: English description"),
            It.IsAny<IPlatformPublishCheckpoint>(),
            It.IsAny<CancellationToken>()));
        _scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.Is<PlatformPublicationAttempt>(attempt =>
                attempt.ContentSnapshot.Title == "English title on 2026-06-25" &&
                attempt.ContentSnapshot.Description == "Details: English description"),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_TemplateReferenceKeyToken_RendersPublishedExternalResourceIdBeforePublishing()
    {
        var wordpressPlatform = Platform(
            "Company blog",
            PlatformType.WordPress,
            WordPressPublishSettings,
            new PublishingContent(
                "wordpress-title-template",
                "wordpress-description-template"));
        var youtubePlatform = Platform(
            YouTubePlatformId,
            "Private YouTube channel",
            PlatformType.YouTube,
            YouTubePublishSettings,
            referenceKey: "privateYouTube");
        _scenario.SelectedPlatform = wordpressPlatform;
        _scenario.ActivePlatforms = [wordpressPlatform, youtubePlatform];
        _scenario.PublicationRows =
        [
            Publication(
                PublishStatus.Published,
                platformId: YouTubePlatformId,
                externalResourceId: "yt-broadcast-id")
        ];
        SetTemplates(
            new TemplateView(
                "wordpress-title-template",
                "WordPress title",
                TemplateType.WordPress,
                "{{ text1 }}"),
            new TemplateView(
                "wordpress-description-template",
                "WordPress description",
                TemplateType.WordPress,
                "YouTube BroadcastId: {{ privateYouTube }}"));
        SetPublisher(PlatformType.WordPress, "123");

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        _scenario.Publisher.Verify(candidate => candidate.PublishAsync(
            It.Is<PlatformPublishRequest>(request =>
                request.Description == "YouTube BroadcastId: yt-broadcast-id"),
            It.IsAny<IPlatformPublishCheckpoint>(),
            It.IsAny<CancellationToken>()));
        _scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.Is<PlatformPublicationAttempt>(attempt =>
                attempt.ContentSnapshot.Description ==
                    "YouTube BroadcastId: yt-broadcast-id"),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_ReferenceKeyTokenWithoutPublishedValue_ReturnsInvalidPublishingContent()
    {
        var wordpressPlatform = Platform(
            "Company blog",
            PlatformType.WordPress,
            WordPressPublishSettings,
            new PublishingContent(
                "wordpress-title-template",
                "wordpress-description-template"));
        var youtubePlatform = Platform(
            YouTubePlatformId,
            "Private YouTube channel",
            PlatformType.YouTube,
            YouTubePublishSettings,
            referenceKey: "privateYouTube");
        _scenario.SelectedPlatform = wordpressPlatform;
        _scenario.ActivePlatforms = [wordpressPlatform, youtubePlatform];
        SetTemplates(
            new TemplateView(
                "wordpress-title-template",
                "WordPress title",
                TemplateType.WordPress,
                "{{ text1 }}"),
            new TemplateView(
                "wordpress-description-template",
                "WordPress description",
                TemplateType.WordPress,
                "YouTube BroadcastId: {{ privateYouTube }}"));
        SetPublisher(PlatformType.WordPress, "123");

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt();
    }

    [Fact]
    public async Task HandleAsync_MissingTemplate_ReturnsInvalidPublishingContent()
    {
        _scenario.SelectedPlatform = Platform(
            new PublishingContent("missing-template", "description-template"));
        _scenario.ActivePlatforms = [_scenario.SelectedPlatform];

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt();
    }

    [Fact]
    public async Task HandleAsync_EmptyRenderedTitle_ReturnsInvalidPublishingContent()
    {
        _scenario.CalendarEvent = Event(FutureStart, Text(description: string.Empty));
        SetTemplates(
            new TemplateView(
                ApplicationTestData.TitleTemplateId,
                "Title template",
                TemplateType.YouTube,
                "{{ text2 }}"),
            new TemplateView(
                ApplicationTestData.DescriptionTemplateId,
                "Description template",
                TemplateType.YouTube,
                "Description"));

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt();
    }

    [Fact]
    public async Task HandleAsync_UnresolvedToken_ReturnsInvalidPublishingContent()
    {
        SetTemplates(
            new TemplateView(
                ApplicationTestData.TitleTemplateId,
                "Title template",
                TemplateType.YouTube,
                "{{ unknownToken }}"),
            new TemplateView(
                ApplicationTestData.DescriptionTemplateId,
                "Description template",
                TemplateType.YouTube,
                "Description"));

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt();
    }

    private void SetTemplates(params TemplateView[] templates)
    {
        foreach (var template in templates)
        {
            _scenario.Templates
                .Setup(candidate => candidate.GetAsync(
                    template.Type,
                    template.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(template);
        }
    }

    private void SetPublisher(
        PlatformType type,
        string externalResourceId)
    {
        _scenario.Publisher.SetupGet(candidate => candidate.Type).Returns(type);
        _scenario.Publisher
            .Setup(candidate => candidate.PublishAsync(
                It.IsAny<PlatformPublishRequest>(),
                It.IsAny<IPlatformPublishCheckpoint>(),
                It.IsAny<CancellationToken>()))
            .Returns<PlatformPublishRequest, IPlatformPublishCheckpoint, CancellationToken>(
                async (_, checkpoint, cancellationToken) =>
                {
                    await checkpoint.SaveExternalResourceIdAsync(
                        externalResourceId,
                        cancellationToken);
                    return new PlatformPublishResult(externalResourceId);
                });
    }

    private void VerifyNoPublishAttempt()
    {
        _scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.IsAny<PlatformPublicationAttempt>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _scenario.Publisher.Verify(candidate => candidate.PublishAsync(
            It.IsAny<PlatformPublishRequest>(),
            It.IsAny<IPlatformPublishCheckpoint>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }
}

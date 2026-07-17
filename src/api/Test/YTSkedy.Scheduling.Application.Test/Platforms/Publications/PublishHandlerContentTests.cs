using static YTSkedy.Scheduling.Application.Test.PublishHandlerScenario;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerContentTests
{
    [Fact]
    public async Task HandleAsync_NoTitleText_ReturnsInvalidPublishingContent()
    {
        var scenario = new PublishHandlerScenario
        {
            CalendarEvent = Event(FutureStart, Text(title: null))
        };

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt(scenario);
    }

    [Fact]
    public async Task HandleAsync_BlankTitleText_ReturnsInvalidPublishingContent()
    {
        var scenario = new PublishHandlerScenario
        {
            CalendarEvent = Event(
                FutureStart,
                Text(title: "   ", description: "description"))
        };

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt(scenario);
    }

    [Fact]
    public async Task HandleAsync_TemplateContent_RendersBeforePublishing()
    {
        var scenario = new PublishHandlerScenario();
        scenario.SelectedPlatform = Platform(
            new PublishingContent("title-template", "description-template"));
        scenario.ActivePlatforms = [scenario.SelectedPlatform];
        SetTemplates(
            scenario,
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

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        scenario.Publisher.Verify(candidate => candidate.PublishAsync(
            It.Is<PlatformPublishRequest>(request =>
                request.Title == "English title on 2026-06-25" &&
                request.Description == "Details: English description"),
            It.IsAny<IPlatformPublishCheckpoint>(),
            It.IsAny<CancellationToken>()));
        scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
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
        var scenario = new PublishHandlerScenario
        {
            SelectedPlatform = wordpressPlatform,
            ActivePlatforms = [wordpressPlatform, youtubePlatform],
            PublicationRows =
            [
                Publication(
                    PublishStatus.Published,
                    platformId: YouTubePlatformId,
                    externalResourceId: "yt-broadcast-id")
            ]
        };
        SetTemplates(
            scenario,
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
        SetPublisher(scenario, PlatformType.WordPress, "123");

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        scenario.Publisher.Verify(candidate => candidate.PublishAsync(
            It.Is<PlatformPublishRequest>(request =>
                request.Description == "YouTube BroadcastId: yt-broadcast-id"),
            It.IsAny<IPlatformPublishCheckpoint>(),
            It.IsAny<CancellationToken>()));
        scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
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
        var scenario = new PublishHandlerScenario
        {
            SelectedPlatform = wordpressPlatform,
            ActivePlatforms = [wordpressPlatform, youtubePlatform]
        };
        SetTemplates(
            scenario,
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
        SetPublisher(scenario, PlatformType.WordPress, "123");

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt(scenario);
    }

    [Fact]
    public async Task HandleAsync_MissingTemplate_ReturnsInvalidPublishingContent()
    {
        var scenario = new PublishHandlerScenario();
        scenario.SelectedPlatform = Platform(
            new PublishingContent("missing-template", "description-template"));
        scenario.ActivePlatforms = [scenario.SelectedPlatform];

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt(scenario);
    }

    [Fact]
    public async Task HandleAsync_EmptyRenderedTitle_ReturnsInvalidPublishingContent()
    {
        var scenario = new PublishHandlerScenario
        {
            CalendarEvent = Event(FutureStart, Text(description: string.Empty))
        };
        SetTemplates(
            scenario,
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

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt(scenario);
    }

    [Fact]
    public async Task HandleAsync_UnresolvedToken_ReturnsInvalidPublishingContent()
    {
        var scenario = new PublishHandlerScenario();
        SetTemplates(
            scenario,
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

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        VerifyNoPublishAttempt(scenario);
    }

    private static void SetTemplates(
        PublishHandlerScenario scenario,
        params TemplateView[] templates)
    {
        foreach (var template in templates)
        {
            scenario.Templates
                .Setup(candidate => candidate.GetAsync(
                    template.Type,
                    template.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(template);
        }
    }

    private static void SetPublisher(
        PublishHandlerScenario scenario,
        PlatformType type,
        string externalResourceId)
    {
        scenario.Publisher.SetupGet(candidate => candidate.Type).Returns(type);
        scenario.Publisher
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

    private static void VerifyNoPublishAttempt(PublishHandlerScenario scenario)
    {
        scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.IsAny<PlatformPublicationAttempt>(),
            It.IsAny<CancellationToken>()), Times.Never());
        scenario.Publisher.Verify(candidate => candidate.PublishAsync(
            It.IsAny<PlatformPublishRequest>(),
            It.IsAny<IPlatformPublishCheckpoint>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }
}

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
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher();
        var handler = CreateHandler(
            Event(FutureStart, Text(title: null)),
            Platform(),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task HandleAsync_BlankTitleText_ReturnsInvalidPublishingContent()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher();
        var handler = CreateHandler(
            Event(FutureStart, Text(title: "   ", description: "description")),
            Platform(),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task HandleAsync_TemplateContent_RendersBeforePublishing()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Result = new PlatformPublishResult("yt-broadcast-id")
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(new PublishingContent("title-template", "description-template")),
            publisher,
            repository: repository,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "title-template",
                    "Title template",
                    TemplateType.YouTube,
                    "{{ text1 }} on {{ shortDateEn }}"),
                new TemplateView(
                    "description-template",
                    "Description template",
                    TemplateType.YouTube,
                    "Details: {{ text2 }}")));

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.Equal("English title on 2026-06-25", publisher.Request!.Title);
        Assert.Equal("Details: English description", publisher.Request.Description);
        Assert.Equal("English title on 2026-06-25", repository.StartedAttempt!.ContentSnapshot.Title);
        Assert.Equal("Details: English description", repository.StartedAttempt.ContentSnapshot.Description);
    }

    [Fact]
    public async Task HandleAsync_TemplateReferenceKeyToken_RendersPublishedExternalResourceIdBeforePublishing()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher(
            PlatformType.WordPress,
            new PlatformPublishResult("123"));
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
        var handler = CreateHandler(
            Event(FutureStart),
            wordpressPlatform,
            publisher,
            repository: repository,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "wordpress-title-template",
                    "WordPress title",
                    TemplateType.WordPress,
                    "{{ text1 }}"),
                new TemplateView(
                    "wordpress-description-template",
                    "WordPress description",
                    TemplateType.WordPress,
                    "YouTube BroadcastId: {{ privateYouTube }}")),
            activePlatforms: [wordpressPlatform, youtubePlatform],
            publicationRows:
            [
                Publication(
                    PublishStatus.Published,
                    platformId: YouTubePlatformId,
                    externalResourceId: "yt-broadcast-id")
            ]);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.Equal("YouTube BroadcastId: yt-broadcast-id", publisher.Request!.Description);
        Assert.Equal(
            "YouTube BroadcastId: yt-broadcast-id",
            repository.StartedAttempt!.ContentSnapshot.Description);
    }

    [Fact]
    public async Task HandleAsync_ReferenceKeyTokenWithoutPublishedValue_ReturnsInvalidPublishingContent()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher(PlatformType.WordPress);
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
        var handler = CreateHandler(
            Event(FutureStart),
            wordpressPlatform,
            publisher,
            repository: repository,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "wordpress-title-template",
                    "WordPress title",
                    TemplateType.WordPress,
                    "{{ text1 }}"),
                new TemplateView(
                    "wordpress-description-template",
                    "WordPress description",
                    TemplateType.WordPress,
                    "YouTube BroadcastId: {{ privateYouTube }}")),
            activePlatforms: [wordpressPlatform, youtubePlatform],
            publicationRows: []);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task HandleAsync_MissingTemplate_ReturnsInvalidPublishingContent()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(new PublishingContent("missing-template", "description-template")),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task HandleAsync_EmptyRenderedTitle_ReturnsInvalidPublishingContent()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher();
        var handler = CreateHandler(
            Event(FutureStart, Text(description: string.Empty)),
            Platform(),
            publisher,
            repository: repository,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "title-template",
                    "Title template",
                    TemplateType.YouTube,
                    "{{ text2 }}"),
                new TemplateView(
                    "description-template",
                    "Description template",
                    TemplateType.YouTube,
                    "Description")));

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task HandleAsync_UnresolvedToken_ReturnsInvalidPublishingContent()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "title-template",
                    "Title template",
                    TemplateType.YouTube,
                    "{{ unknownToken }}"),
                new TemplateView(
                    "description-template",
                    "Description template",
                    TemplateType.YouTube,
                    "Description")));

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }
}

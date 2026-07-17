using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class CreatePlatformHandlerTests
{
    private static readonly YouTubeSettings Settings =
        ApplicationTestData.YouTubeSettings();

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesPlatformAndReturnsCreatedWithId()
    {
        var modifier = new Mock<IPlatformModifier>();
        modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<Platform>(),
                CancellationToken.None))
            .ReturnsAsync(CreatePlatformResult.Created("p1"));
        var templates = RequiredTemplateReader();
        var publishingContent = ApplicationTestData.PublishingContent();
        var handler = new CreatePlatformHandler(modifier.Object, templates.Object);
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            publishingContent,
            "main-youtube");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.Created, result.Status);
        Assert.Equal("p1", result.PlatformId);

        modifier.Verify(candidate => candidate.CreateAsync(
            It.Is<Platform>(platform =>
                platform.Name == "Main channel" &&
                platform.Type == PlatformType.YouTube &&
                ReferenceEquals(platform.PublishSettings, Settings) &&
                platform.ReferenceKey == "main-youtube" &&
                ReferenceEquals(platform.PublishingContent, publishingContent)),
            CancellationToken.None));
        VerifyRequiredTemplates(templates);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var modifier = new Mock<IPlatformModifier>();
        modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<Platform>(),
                CancellationToken.None))
            .ReturnsAsync(CreatePlatformResult.NameAlreadyExists());
        var templates = RequiredTemplateReader();
        var handler = new CreatePlatformHandler(modifier.Object, templates.Object);
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.NameAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
        VerifyRequiredTemplates(templates);
    }

    [Fact]
    public async Task HandleAsync_DuplicateReferenceKey_ReturnsReferenceKeyAlreadyExists()
    {
        var modifier = new Mock<IPlatformModifier>();
        modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<Platform>(),
                CancellationToken.None))
            .ReturnsAsync(CreatePlatformResult.ReferenceKeyAlreadyExists());
        var handler = new CreatePlatformHandler(
            modifier.Object,
            RequiredTemplateReader().Object);
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            ApplicationTestData.PublishingContent(),
            "main-youtube");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.ReferenceKeyAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
    }

    [Fact]
    public async Task HandleAsync_LinkedTemplateMissing_ReturnsLinkedTemplateNotFound()
    {
        var modifier = new Mock<IPlatformModifier>();
        var templates = new Mock<ITemplateReader>();
        templates
            .Setup(candidate => candidate.GetAsync(
                TemplateType.YouTube,
                "missing-template",
                CancellationToken.None))
            .ReturnsAsync((TemplateView?)null);
        var handler = new CreatePlatformHandler(modifier.Object, templates.Object);
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            ApplicationTestData.PublishingContent("missing-template"));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.LinkedTemplateNotFound, result.Status);
        Assert.Null(result.PlatformId);
        modifier.Verify(candidate => candidate.CreateAsync(
            It.IsAny<Platform>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var modifier = new Mock<IPlatformModifier>();
        var handler = new CreatePlatformHandler(
            modifier.Object,
            new Mock<ITemplateReader>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));

        modifier.Verify(candidate => candidate.CreateAsync(
            It.IsAny<Platform>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    private static Mock<ITemplateReader> RequiredTemplateReader()
    {
        var reader = new Mock<ITemplateReader>();
        foreach (var template in ApplicationTestData.RequiredTemplates())
        {
            reader
                .Setup(candidate => candidate.GetAsync(
                    template.Type,
                    template.Id,
                    CancellationToken.None))
                .ReturnsAsync(template);
        }

        return reader;
    }

    private static void VerifyRequiredTemplates(Mock<ITemplateReader> reader)
    {
        reader.Verify(candidate => candidate.GetAsync(
            TemplateType.YouTube,
            ApplicationTestData.TitleTemplateId,
            CancellationToken.None));
        reader.Verify(candidate => candidate.GetAsync(
            TemplateType.YouTube,
            ApplicationTestData.DescriptionTemplateId,
            CancellationToken.None));
    }
}

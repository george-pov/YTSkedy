using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class CreatePlatformHandlerTests
{
    private static readonly YouTubeSettings Settings =
        ApplicationTestData.YouTubeSettings();
    private readonly Mock<ITemplateReader> _templates = new();
    private readonly Mock<IPlatformModifier> _modifier = new();
    private readonly CreatePlatformHandler _handler;

    public CreatePlatformHandlerTests()
    {
        _handler = new CreatePlatformHandler(_modifier.Object, _templates.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesPlatformAndReturnsCreatedWithId()
    {
        _modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<Platform>(),
                CancellationToken.None))
            .ReturnsAsync(CreatePlatformResult.Created("p1"));
        var templates = RequiredTemplateReader();
        var publishingContent = ApplicationTestData.PublishingContent();
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            publishingContent,
            "main-youtube");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.Created, result.Status);
        Assert.Equal("p1", result.PlatformId);

        _modifier.Verify(candidate => candidate.CreateAsync(
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
        _modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<Platform>(),
                CancellationToken.None))
            .ReturnsAsync(CreatePlatformResult.NameAlreadyExists());
        var templates = RequiredTemplateReader();
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.NameAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
        VerifyRequiredTemplates(templates);
    }

    [Fact]
    public async Task HandleAsync_DuplicateReferenceKey_ReturnsReferenceKeyAlreadyExists()
    {
        _modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<Platform>(),
                CancellationToken.None))
            .ReturnsAsync(CreatePlatformResult.ReferenceKeyAlreadyExists());
        RequiredTemplateReader();
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            ApplicationTestData.PublishingContent(),
            "main-youtube");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.ReferenceKeyAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
    }

    [Fact]
    public async Task HandleAsync_LinkedTemplateMissing_ReturnsLinkedTemplateNotFound()
    {
        _templates
            .Setup(candidate => candidate.GetAsync(
                TemplateType.YouTube,
                "missing-template",
                CancellationToken.None))
            .ReturnsAsync((TemplateView?)null);
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            ApplicationTestData.PublishingContent("missing-template"));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.LinkedTemplateNotFound, result.Status);
        Assert.Null(result.PlatformId);
        _modifier.Verify(candidate => candidate.CreateAsync(
            It.IsAny<Platform>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));

        _modifier.Verify(candidate => candidate.CreateAsync(
            It.IsAny<Platform>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    private Mock<ITemplateReader> RequiredTemplateReader()
    {
        foreach (var template in ApplicationTestData.RequiredTemplates())
        {
            _templates
                .Setup(candidate => candidate.GetAsync(
                    template.Type,
                    template.Id,
                    CancellationToken.None))
                .ReturnsAsync(template);
        }

        return _templates;
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

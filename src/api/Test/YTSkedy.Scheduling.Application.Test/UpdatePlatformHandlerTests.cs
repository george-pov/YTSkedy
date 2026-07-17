using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdatePlatformHandlerTests
{
    private static readonly YouTubeSettings Settings =
        ApplicationTestData.YouTubeSettings("unlisted");
    private readonly Mock<IPlatformReader> _platforms = new();
    private readonly Mock<ITemplateReader> _templates = new();
    private readonly Mock<IPlatformModifier> _modifier = new();
    private readonly UpdatePlatformHandler _handler;

    public UpdatePlatformHandlerTests()
    {
        _handler = new UpdatePlatformHandler(
            _platforms.Object,
            _modifier.Object,
            _templates.Object);
    }

    [Fact]
    public async Task HandleAsync_Updated_ForwardsCommandAndReturnsUpdated()
    {
        _modifier
            .Setup(candidate => candidate.UpdateAsync(
                "p1",
                "Renamed channel",
                "main-youtube",
                Settings,
                It.IsAny<PublishingContent>(),
                CancellationToken.None))
            .ReturnsAsync(UpdatePlatformResult.Updated);
        var templates = RequiredTemplateReader();
        var publishingContent = ApplicationTestData.PublishingContent();
        PlatformReader(ExistingPlatform());
        var command = new UpdatePlatformCommand(
            "p1",
            "Renamed channel",
            "main-youtube",
            Settings,
            publishingContent);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.Updated, result);
        _modifier.Verify(candidate => candidate.UpdateAsync(
            "p1",
            "Renamed channel",
            "main-youtube",
            Settings,
            publishingContent,
            CancellationToken.None));
        VerifyRequiredTemplates(templates);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNotFound()
    {
        PlatformReader(null);
        var command = new UpdatePlatformCommand(
            "missing",
            "Renamed channel",
            null,
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.NotFound, result);
        VerifyNoUpdate();
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        _modifier
            .Setup(candidate => candidate.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<PublishSettings>(),
                It.IsAny<PublishingContent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdatePlatformResult.NameAlreadyExists);
        PlatformReader(ExistingPlatform());
        RequiredTemplateReader();
        var command = new UpdatePlatformCommand(
            "p1",
            "Taken name",
            null,
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.NameAlreadyExists, result);
    }

    [Fact]
    public async Task HandleAsync_DuplicateReferenceKey_ReturnsReferenceKeyAlreadyExists()
    {
        _modifier
            .Setup(candidate => candidate.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<PublishSettings>(),
                It.IsAny<PublishingContent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdatePlatformResult.ReferenceKeyAlreadyExists);
        PlatformReader(ExistingPlatform());
        RequiredTemplateReader();
        var command = new UpdatePlatformCommand(
            "p1",
            "Main channel",
            "taken-key",
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.ReferenceKeyAlreadyExists, result);
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
        PlatformReader(ExistingPlatform());
        var command = new UpdatePlatformCommand(
            "p1",
            "Main channel",
            null,
            Settings,
            ApplicationTestData.PublishingContent("missing-template"));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.LinkedTemplateNotFound, result);
        VerifyNoUpdate();
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        PlatformReader(ExistingPlatform());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));
    }

    private static PlatformView ExistingPlatform() =>
        ApplicationTestData.Platform(
            platformId: "p1",
            name: "Main channel",
            referenceKey: "main-youtube",
            publishSettings: Settings);

    private Mock<IPlatformReader> PlatformReader(PlatformView? platform)
    {
        _platforms
            .Setup(candidate => candidate.GetAsync(
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(platform);
        return _platforms;
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

    private void VerifyNoUpdate() =>
        _modifier.Verify(candidate => candidate.UpdateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<PublishSettings>(),
            It.IsAny<PublishingContent>(),
            It.IsAny<CancellationToken>()), Times.Never());
}

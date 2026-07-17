using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdatePlatformHandlerTests
{
    private static readonly YouTubeSettings Settings =
        ApplicationTestData.YouTubeSettings("unlisted");

    [Fact]
    public async Task HandleAsync_Updated_ForwardsCommandAndReturnsUpdated()
    {
        var modifier = new Mock<IPlatformModifier>();
        modifier
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
        var handler = new UpdatePlatformHandler(
            PlatformReader(ExistingPlatform()).Object,
            modifier.Object,
            templates.Object);
        var command = new UpdatePlatformCommand(
            "p1",
            "Renamed channel",
            "main-youtube",
            Settings,
            publishingContent);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.Updated, result);
        modifier.Verify(candidate => candidate.UpdateAsync(
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
        var modifier = new Mock<IPlatformModifier>();
        var handler = new UpdatePlatformHandler(
            PlatformReader(null).Object,
            modifier.Object,
            new Mock<ITemplateReader>().Object);
        var command = new UpdatePlatformCommand(
            "missing",
            "Renamed channel",
            null,
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.NotFound, result);
        VerifyNoUpdate(modifier);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var modifier = new Mock<IPlatformModifier>();
        modifier
            .Setup(candidate => candidate.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<PublishSettings>(),
                It.IsAny<PublishingContent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdatePlatformResult.NameAlreadyExists);
        var handler = new UpdatePlatformHandler(
            PlatformReader(ExistingPlatform()).Object,
            modifier.Object,
            RequiredTemplateReader().Object);
        var command = new UpdatePlatformCommand(
            "p1",
            "Taken name",
            null,
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.NameAlreadyExists, result);
    }

    [Fact]
    public async Task HandleAsync_DuplicateReferenceKey_ReturnsReferenceKeyAlreadyExists()
    {
        var modifier = new Mock<IPlatformModifier>();
        modifier
            .Setup(candidate => candidate.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<PublishSettings>(),
                It.IsAny<PublishingContent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdatePlatformResult.ReferenceKeyAlreadyExists);
        var handler = new UpdatePlatformHandler(
            PlatformReader(ExistingPlatform()).Object,
            modifier.Object,
            RequiredTemplateReader().Object);
        var command = new UpdatePlatformCommand(
            "p1",
            "Main channel",
            "taken-key",
            Settings,
            ApplicationTestData.PublishingContent());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.ReferenceKeyAlreadyExists, result);
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
        var handler = new UpdatePlatformHandler(
            PlatformReader(ExistingPlatform()).Object,
            modifier.Object,
            templates.Object);
        var command = new UpdatePlatformCommand(
            "p1",
            "Main channel",
            null,
            Settings,
            ApplicationTestData.PublishingContent("missing-template"));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.LinkedTemplateNotFound, result);
        VerifyNoUpdate(modifier);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new UpdatePlatformHandler(
            PlatformReader(ExistingPlatform()).Object,
            new Mock<IPlatformModifier>().Object,
            new Mock<ITemplateReader>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private static PlatformView ExistingPlatform() =>
        ApplicationTestData.Platform(
            platformId: "p1",
            name: "Main channel",
            referenceKey: "main-youtube",
            publishSettings: Settings);

    private static Mock<IPlatformReader> PlatformReader(PlatformView? platform)
    {
        var reader = new Mock<IPlatformReader>();
        reader
            .Setup(candidate => candidate.GetAsync(
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(platform);
        return reader;
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

    private static void VerifyNoUpdate(Mock<IPlatformModifier> modifier) =>
        modifier.Verify(candidate => candidate.UpdateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<PublishSettings>(),
            It.IsAny<PublishingContent>(),
            It.IsAny<CancellationToken>()), Times.Never());
}

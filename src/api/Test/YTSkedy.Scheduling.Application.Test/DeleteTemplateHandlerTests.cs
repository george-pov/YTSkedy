using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class DeleteTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_ForwardsToModifierAndReturnsResult()
    {
        var modifier = new Mock<ITemplateModifier>();
        modifier
            .Setup(candidate => candidate.DeleteAsync(
                TemplateType.YouTube,
                "9f8b1c2d3e4f",
                CancellationToken.None))
            .ReturnsAsync(DeleteTemplateResult.Deleted);
        var platforms = new Mock<IPlatformReader>();
        platforms
            .Setup(reader => reader.ListAsync(
                PlatformType.YouTube,
                CancellationToken.None))
            .ReturnsAsync([]);
        var handler = new DeleteTemplateHandler(modifier.Object, platforms.Object);
        var command = new DeleteTemplateCommand(TemplateType.YouTube, "9f8b1c2d3e4f");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(DeleteTemplateResult.Deleted, result);
        modifier.Verify(candidate => candidate.DeleteAsync(
            TemplateType.YouTube,
            "9f8b1c2d3e4f",
            CancellationToken.None));
        platforms.Verify(reader => reader.ListAsync(
            PlatformType.YouTube,
            CancellationToken.None));
    }

    [Theory]
    [InlineData(DeleteTemplateResult.Deleted)]
    [InlineData(DeleteTemplateResult.NotFound)]
    public async Task HandleAsync_ModifierResult_IsReturnedUnchanged(
        DeleteTemplateResult modifierResult)
    {
        var modifier = new Mock<ITemplateModifier>();
        modifier
            .Setup(candidate => candidate.DeleteAsync(
                It.IsAny<TemplateType>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(modifierResult);
        var platforms = new Mock<IPlatformReader>();
        platforms
            .Setup(reader => reader.ListAsync(
                PlatformType.WordPress,
                CancellationToken.None))
            .ReturnsAsync([]);
        var handler = new DeleteTemplateHandler(modifier.Object, platforms.Object);
        var command = new DeleteTemplateCommand(TemplateType.WordPress, "9f8b1c2d3e4f");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(modifierResult, result);
    }

    [Fact]
    public async Task HandleAsync_TemplateReferencedByPlatform_ReturnsReferencedByPlatform()
    {
        var modifier = new Mock<ITemplateModifier>();
        var platforms = new Mock<IPlatformReader>();
        platforms
            .Setup(reader => reader.ListAsync(
                PlatformType.YouTube,
                CancellationToken.None))
            .ReturnsAsync([
                ApplicationTestData.Platform(
                    platformId: "p1",
                    name: "Main channel",
                    publishingContent: new PublishingContent(
                        "9f8b1c2d3e4f",
                        "description-template"))
            ]);
        var handler = new DeleteTemplateHandler(modifier.Object, platforms.Object);
        var command = new DeleteTemplateCommand(TemplateType.YouTube, "9f8b1c2d3e4f");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(DeleteTemplateResult.ReferencedByPlatform, result);
        modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<TemplateType>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var modifier = new Mock<ITemplateModifier>();
        var handler = new DeleteTemplateHandler(
            modifier.Object,
            new Mock<IPlatformReader>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));

        modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<TemplateType>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }
}

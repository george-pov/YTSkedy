using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class DeleteTemplateHandlerTests
{
    private readonly Mock<ITemplateModifier> _modifier = new();
    private readonly Mock<IPlatformReader> _platforms = new();
    private readonly DeleteTemplateHandler _handler;

    public DeleteTemplateHandlerTests()
    {
        _handler = new DeleteTemplateHandler(_modifier.Object, _platforms.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ForwardsToModifierAndReturnsResult()
    {
        _modifier
            .Setup(candidate => candidate.DeleteAsync(
                TemplateType.YouTube,
                "9f8b1c2d3e4f",
                CancellationToken.None))
            .ReturnsAsync(DeleteTemplateResult.Deleted);
        _platforms
            .Setup(reader => reader.ListAsync(
                PlatformType.YouTube,
                CancellationToken.None))
            .ReturnsAsync([]);
        var command = new DeleteTemplateCommand(TemplateType.YouTube, "9f8b1c2d3e4f");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(DeleteTemplateResult.Deleted, result);
        _modifier.Verify(candidate => candidate.DeleteAsync(
            TemplateType.YouTube,
            "9f8b1c2d3e4f",
            CancellationToken.None));
        _platforms.Verify(reader => reader.ListAsync(
            PlatformType.YouTube,
            CancellationToken.None));
    }

    [Theory]
    [InlineData(DeleteTemplateResult.Deleted)]
    [InlineData(DeleteTemplateResult.NotFound)]
    public async Task HandleAsync_ModifierResult_IsReturnedUnchanged(
        DeleteTemplateResult modifierResult)
    {
        _modifier
            .Setup(candidate => candidate.DeleteAsync(
                It.IsAny<TemplateType>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(modifierResult);
        _platforms
            .Setup(reader => reader.ListAsync(
                PlatformType.WordPress,
                CancellationToken.None))
            .ReturnsAsync([]);
        var command = new DeleteTemplateCommand(TemplateType.WordPress, "9f8b1c2d3e4f");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(modifierResult, result);
    }

    [Fact]
    public async Task HandleAsync_TemplateReferencedByPlatform_ReturnsReferencedByPlatform()
    {
        _platforms
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
        var command = new DeleteTemplateCommand(TemplateType.YouTube, "9f8b1c2d3e4f");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(DeleteTemplateResult.ReferencedByPlatform, result);
        _modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<TemplateType>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));

        _modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<TemplateType>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }
}

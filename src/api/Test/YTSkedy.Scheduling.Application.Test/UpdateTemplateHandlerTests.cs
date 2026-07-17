using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdateTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_ForwardsToModifierAndReturnsResult()
    {
        var modifier = new Mock<ITemplateModifier>();
        modifier
            .Setup(candidate => candidate.UpdateAsync(
                TemplateType.YouTube,
                "9f8b1c2d3e4f",
                "Renamed",
                "Updated content",
                CancellationToken.None))
            .ReturnsAsync(UpdateTemplateResult.Updated);
        var handler = new UpdateTemplateHandler(modifier.Object);
        var command = new UpdateTemplateCommand(
            TemplateType.YouTube,
            "9f8b1c2d3e4f",
            "Renamed",
            "Updated content");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdateTemplateResult.Updated, result);
        modifier.Verify(candidate => candidate.UpdateAsync(
            TemplateType.YouTube,
            "9f8b1c2d3e4f",
            "Renamed",
            "Updated content",
            CancellationToken.None));
    }

    [Theory]
    [InlineData(UpdateTemplateResult.Updated)]
    [InlineData(UpdateTemplateResult.NotFound)]
    [InlineData(UpdateTemplateResult.NameAlreadyExists)]
    public async Task HandleAsync_ModifierResult_IsReturnedUnchanged(
        UpdateTemplateResult modifierResult)
    {
        var modifier = new Mock<ITemplateModifier>();
        modifier
            .Setup(candidate => candidate.UpdateAsync(
                It.IsAny<TemplateType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(modifierResult);
        var handler = new UpdateTemplateHandler(modifier.Object);
        var command = new UpdateTemplateCommand(
            TemplateType.WordPress,
            "9f8b1c2d3e4f",
            "name",
            "content");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(modifierResult, result);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var modifier = new Mock<ITemplateModifier>();
        var handler = new UpdateTemplateHandler(modifier.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));

        modifier.Verify(candidate => candidate.UpdateAsync(
            It.IsAny<TemplateType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }
}

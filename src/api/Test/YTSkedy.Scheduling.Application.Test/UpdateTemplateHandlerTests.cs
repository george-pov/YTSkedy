using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdateTemplateHandlerTests
{
    private readonly Mock<ITemplateModifier> _modifier = new();
    private readonly UpdateTemplateHandler _handler;

    public UpdateTemplateHandlerTests()
    {
        _handler = new UpdateTemplateHandler(_modifier.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ForwardsToModifierAndReturnsResult()
    {
        _modifier
            .Setup(candidate => candidate.UpdateAsync(
                TemplateType.YouTube,
                "9f8b1c2d3e4f",
                "Renamed",
                "Updated content",
                CancellationToken.None))
            .ReturnsAsync(UpdateTemplateResult.Updated);
        var command = new UpdateTemplateCommand(
            TemplateType.YouTube,
            "9f8b1c2d3e4f",
            "Renamed",
            "Updated content");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdateTemplateResult.Updated, result);
        _modifier.Verify(candidate => candidate.UpdateAsync(
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
        _modifier
            .Setup(candidate => candidate.UpdateAsync(
                It.IsAny<TemplateType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(modifierResult);
        var command = new UpdateTemplateCommand(
            TemplateType.WordPress,
            "9f8b1c2d3e4f",
            "name",
            "content");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(modifierResult, result);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));

        _modifier.Verify(candidate => candidate.UpdateAsync(
            It.IsAny<TemplateType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }
}

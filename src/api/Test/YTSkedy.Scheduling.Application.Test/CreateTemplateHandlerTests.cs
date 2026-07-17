using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class CreateTemplateHandlerTests
{
    private readonly Mock<ITemplateModifier> _modifier = new();
    private readonly CreateTemplateHandler _handler;

    public CreateTemplateHandlerTests()
    {
        _handler = new CreateTemplateHandler(_modifier.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesTemplateAndReturnsCreatedWithId()
    {
        _modifier
            .Setup(candidate => candidate.CreateAsync(
                It.Is<Template>(template =>
                    template.Name == "Weeknight stream" &&
                    template.Type == TemplateType.YouTube &&
                    template.Content == "Live on {{ longDateEn }}"),
                CancellationToken.None))
            .ReturnsAsync(CreateTemplateResult.Created("9f8b1c2d3e4f"));
        var command = new CreateTemplateCommand(
            "Weeknight stream",
            TemplateType.YouTube,
            "Live on {{ longDateEn }}");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateTemplateStatus.Created, result.Status);
        Assert.Equal("9f8b1c2d3e4f", result.TemplateId);

        _modifier.Verify(candidate => candidate.CreateAsync(
            It.Is<Template>(template =>
                template.Name == "Weeknight stream" &&
                template.Type == TemplateType.YouTube &&
                template.Content == "Live on {{ longDateEn }}"),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        _modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<Template>(),
                CancellationToken.None))
            .ReturnsAsync(CreateTemplateResult.NameAlreadyExists());
        var command = new CreateTemplateCommand(
            "Weeknight stream",
            TemplateType.YouTube,
            "content");

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateTemplateStatus.NameAlreadyExists, result.Status);
        Assert.Null(result.TemplateId);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));

        _modifier.Verify(candidate => candidate.CreateAsync(
            It.IsAny<Template>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }
}

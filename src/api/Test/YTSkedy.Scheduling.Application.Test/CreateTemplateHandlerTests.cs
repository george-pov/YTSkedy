using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class CreateTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesTemplateAndReturnsCreatedWithId()
    {
        var modifier = new Mock<ITemplateModifier>();
        modifier
            .Setup(candidate => candidate.CreateAsync(
                It.Is<Template>(template =>
                    template.Name == "Weeknight stream" &&
                    template.Type == TemplateType.YouTube &&
                    template.Content == "Live on {{ longDateEn }}"),
                CancellationToken.None))
            .ReturnsAsync(CreateTemplateResult.Created("9f8b1c2d3e4f"));
        var handler = new CreateTemplateHandler(modifier.Object);
        var command = new CreateTemplateCommand(
            "Weeknight stream",
            TemplateType.YouTube,
            "Live on {{ longDateEn }}");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateTemplateStatus.Created, result.Status);
        Assert.Equal("9f8b1c2d3e4f", result.TemplateId);

        modifier.Verify(candidate => candidate.CreateAsync(
            It.Is<Template>(template =>
                template.Name == "Weeknight stream" &&
                template.Type == TemplateType.YouTube &&
                template.Content == "Live on {{ longDateEn }}"),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var modifier = new Mock<ITemplateModifier>();
        modifier
            .Setup(candidate => candidate.CreateAsync(
                It.IsAny<Template>(),
                CancellationToken.None))
            .ReturnsAsync(CreateTemplateResult.NameAlreadyExists());
        var handler = new CreateTemplateHandler(modifier.Object);
        var command = new CreateTemplateCommand(
            "Weeknight stream",
            TemplateType.YouTube,
            "content");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateTemplateStatus.NameAlreadyExists, result.Status);
        Assert.Null(result.TemplateId);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var modifier = new Mock<ITemplateModifier>();
        var handler = new CreateTemplateHandler(modifier.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));

        modifier.Verify(candidate => candidate.CreateAsync(
            It.IsAny<Template>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }
}

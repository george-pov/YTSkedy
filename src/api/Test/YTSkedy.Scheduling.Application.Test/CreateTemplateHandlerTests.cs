using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class CreateTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesTemplateAndReturnsCreatedWithId()
    {
        var modifier = new FakeTemplateModifier
        {
            CreateResult = CreateTemplateResult.Created("9f8b1c2d3e4f")
        };
        var handler = new CreateTemplateHandler(modifier);
        var command = new CreateTemplateCommand(
            "Weeknight stream",
            TemplateType.YouTube,
            "Live at {{ localizedTime }}");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateTemplateStatus.Created, result.Status);
        Assert.Equal("9f8b1c2d3e4f", result.TemplateId);

        Assert.NotNull(modifier.CreatedTemplate);
        Assert.Equal("Weeknight stream", modifier.CreatedTemplate!.Name);
        Assert.Equal(TemplateType.YouTube, modifier.CreatedTemplate.Type);
        Assert.Equal("Live at {{ localizedTime }}", modifier.CreatedTemplate.Content);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var modifier = new FakeTemplateModifier
        {
            CreateResult = CreateTemplateResult.NameAlreadyExists()
        };
        var handler = new CreateTemplateHandler(modifier);
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
        var handler = new CreateTemplateHandler(new FakeTemplateModifier());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakeTemplateModifier : ITemplateModifier
    {
        public CreateTemplateResult CreateResult { get; init; } =
            CreateTemplateResult.Created("template-id");

        public Template? CreatedTemplate { get; private set; }

        public Task<CreateTemplateResult> CreateAsync(
            Template template,
            CancellationToken cancellationToken)
        {
            CreatedTemplate = template;

            return Task.FromResult(CreateResult);
        }

        public Task<UpdateTemplateResult> UpdateAsync(
            TemplateType type,
            string id,
            string name,
            string content,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeleteTemplateResult> DeleteAsync(
            TemplateType type,
            string id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

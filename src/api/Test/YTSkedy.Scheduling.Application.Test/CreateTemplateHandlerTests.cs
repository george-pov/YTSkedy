using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class CreateTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesTemplateAndReturnsCreatedWithId()
    {
        var repository = new FakeTemplateRepository
        {
            CreateResult = CreateTemplateResult.Created("9f8b1c2d3e4f")
        };
        var handler = new CreateTemplateHandler(repository);
        var command = new CreateTemplateCommand(
            "Weeknight stream",
            TemplateType.YouTube,
            "Live at {{ localizedTime }}");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreateTemplateStatus.Created, result.Status);
        Assert.Equal("9f8b1c2d3e4f", result.TemplateId);

        Assert.NotNull(repository.CreatedTemplate);
        Assert.Equal("Weeknight stream", repository.CreatedTemplate!.Name);
        Assert.Equal(TemplateType.YouTube, repository.CreatedTemplate.Type);
        Assert.Equal("Live at {{ localizedTime }}", repository.CreatedTemplate.Content);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var repository = new FakeTemplateRepository
        {
            CreateResult = CreateTemplateResult.NameAlreadyExists()
        };
        var handler = new CreateTemplateHandler(repository);
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
        var handler = new CreateTemplateHandler(new FakeTemplateRepository());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakeTemplateRepository : ITemplateRepository
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

using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class DeleteTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_ForwardsToModifierAndReturnsResult()
    {
        var modifier = new FakeTemplateModifier
        {
            DeleteResult = DeleteTemplateResult.Deleted
        };
        var handler = new DeleteTemplateHandler(modifier);
        var command = new DeleteTemplateCommand(TemplateType.YouTube, "9f8b1c2d3e4f");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(DeleteTemplateResult.Deleted, result);
        Assert.Equal(1, modifier.DeleteCallCount);
        Assert.Equal(TemplateType.YouTube, modifier.DeletedType);
        Assert.Equal("9f8b1c2d3e4f", modifier.DeletedId);
    }

    [Theory]
    [InlineData(DeleteTemplateResult.Deleted)]
    [InlineData(DeleteTemplateResult.NotFound)]
    public async Task HandleAsync_ModifierResult_IsReturnedUnchanged(
        DeleteTemplateResult modifierResult)
    {
        var modifier = new FakeTemplateModifier { DeleteResult = modifierResult };
        var handler = new DeleteTemplateHandler(modifier);
        var command = new DeleteTemplateCommand(TemplateType.WordPress, "9f8b1c2d3e4f");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(modifierResult, result);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new DeleteTemplateHandler(new FakeTemplateModifier());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakeTemplateModifier : ITemplateModifier
    {
        public DeleteTemplateResult DeleteResult { get; init; } = DeleteTemplateResult.Deleted;

        public int DeleteCallCount { get; private set; }
        public TemplateType DeletedType { get; private set; }
        public string? DeletedId { get; private set; }

        public Task<CreateTemplateResult> CreateAsync(
            Template template,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

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
            CancellationToken cancellationToken)
        {
            DeleteCallCount++;
            DeletedType = type;
            DeletedId = id;

            return Task.FromResult(DeleteResult);
        }
    }
}

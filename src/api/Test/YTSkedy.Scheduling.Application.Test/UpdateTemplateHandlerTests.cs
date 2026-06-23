using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdateTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_ForwardsToModifierAndReturnsResult()
    {
        var modifier = new FakeTemplateModifier
        {
            UpdateResult = UpdateTemplateResult.Updated
        };
        var handler = new UpdateTemplateHandler(modifier);
        var command = new UpdateTemplateCommand(
            TemplateType.YouTube,
            "9f8b1c2d3e4f",
            "Renamed",
            "Updated content");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdateTemplateResult.Updated, result);
        Assert.Equal(1, modifier.UpdateCallCount);
        Assert.Equal(TemplateType.YouTube, modifier.UpdatedType);
        Assert.Equal("9f8b1c2d3e4f", modifier.UpdatedId);
        Assert.Equal("Renamed", modifier.UpdatedName);
        Assert.Equal("Updated content", modifier.UpdatedContent);
    }

    [Theory]
    [InlineData(UpdateTemplateResult.Updated)]
    [InlineData(UpdateTemplateResult.NotFound)]
    [InlineData(UpdateTemplateResult.NameAlreadyExists)]
    public async Task HandleAsync_ModifierResult_IsReturnedUnchanged(
        UpdateTemplateResult modifierResult)
    {
        var modifier = new FakeTemplateModifier { UpdateResult = modifierResult };
        var handler = new UpdateTemplateHandler(modifier);
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
        var handler = new UpdateTemplateHandler(new FakeTemplateModifier());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakeTemplateModifier : ITemplateModifier
    {
        public UpdateTemplateResult UpdateResult { get; init; } = UpdateTemplateResult.Updated;

        public int UpdateCallCount { get; private set; }
        public TemplateType UpdatedType { get; private set; }
        public string? UpdatedId { get; private set; }
        public string? UpdatedName { get; private set; }
        public string? UpdatedContent { get; private set; }

        public Task<CreateTemplateResult> CreateAsync(
            Template template,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UpdateTemplateResult> UpdateAsync(
            TemplateType type,
            string id,
            string name,
            string content,
            CancellationToken cancellationToken)
        {
            UpdateCallCount++;
            UpdatedType = type;
            UpdatedId = id;
            UpdatedName = name;
            UpdatedContent = content;

            return Task.FromResult(UpdateResult);
        }

        public Task<DeleteTemplateResult> DeleteAsync(
            TemplateType type,
            string id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

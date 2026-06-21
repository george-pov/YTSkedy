using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdateTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_ForwardsToRepositoryAndReturnsResult()
    {
        var repository = new FakeTemplateRepository
        {
            UpdateResult = UpdateTemplateResult.Updated
        };
        var handler = new UpdateTemplateHandler(repository);
        var command = new UpdateTemplateCommand(
            TemplateType.YouTube,
            "9f8b1c2d3e4f",
            "Renamed",
            "Updated content");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdateTemplateResult.Updated, result);
        Assert.Equal(1, repository.UpdateCallCount);
        Assert.Equal(TemplateType.YouTube, repository.UpdatedType);
        Assert.Equal("9f8b1c2d3e4f", repository.UpdatedId);
        Assert.Equal("Renamed", repository.UpdatedName);
        Assert.Equal("Updated content", repository.UpdatedContent);
    }

    [Theory]
    [InlineData(UpdateTemplateResult.Updated)]
    [InlineData(UpdateTemplateResult.NotFound)]
    [InlineData(UpdateTemplateResult.NameAlreadyExists)]
    public async Task HandleAsync_RepositoryResult_IsReturnedUnchanged(
        UpdateTemplateResult repositoryResult)
    {
        var repository = new FakeTemplateRepository { UpdateResult = repositoryResult };
        var handler = new UpdateTemplateHandler(repository);
        var command = new UpdateTemplateCommand(
            TemplateType.WordPress,
            "9f8b1c2d3e4f",
            "name",
            "content");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(repositoryResult, result);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new UpdateTemplateHandler(new FakeTemplateRepository());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakeTemplateRepository : ITemplateRepository
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

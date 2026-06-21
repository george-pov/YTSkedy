using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class DeleteTemplateHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_ForwardsToRepositoryAndReturnsResult()
    {
        var repository = new FakeTemplateRepository
        {
            DeleteResult = DeleteTemplateResult.Deleted
        };
        var handler = new DeleteTemplateHandler(repository);
        var command = new DeleteTemplateCommand(TemplateType.YouTube, "9f8b1c2d3e4f");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(DeleteTemplateResult.Deleted, result);
        Assert.Equal(1, repository.DeleteCallCount);
        Assert.Equal(TemplateType.YouTube, repository.DeletedType);
        Assert.Equal("9f8b1c2d3e4f", repository.DeletedId);
    }

    [Theory]
    [InlineData(DeleteTemplateResult.Deleted)]
    [InlineData(DeleteTemplateResult.NotFound)]
    public async Task HandleAsync_RepositoryResult_IsReturnedUnchanged(
        DeleteTemplateResult repositoryResult)
    {
        var repository = new FakeTemplateRepository { DeleteResult = repositoryResult };
        var handler = new DeleteTemplateHandler(repository);
        var command = new DeleteTemplateCommand(TemplateType.WordPress, "9f8b1c2d3e4f");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(repositoryResult, result);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new DeleteTemplateHandler(new FakeTemplateRepository());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakeTemplateRepository : ITemplateRepository
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

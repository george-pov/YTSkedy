using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class DeletePlatformHandlerTests
{
    [Fact]
    public async Task HandleAsync_Deleted_ForwardsIdAndReturnsDeleted()
    {
        var repository = new FakePlatformRepository
        {
            DeleteResult = DeletePlatformResult.Deleted
        };
        var handler = new DeletePlatformHandler(repository);

        var result = await handler.HandleAsync(
            new DeletePlatformCommand("p1"),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.Deleted, result);
        Assert.Equal("p1", repository.PlatformId);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNotFound()
    {
        var repository = new FakePlatformRepository
        {
            DeleteResult = DeletePlatformResult.NotFound
        };
        var handler = new DeletePlatformHandler(repository);

        var result = await handler.HandleAsync(
            new DeletePlatformCommand("missing"),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.NotFound, result);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new DeletePlatformHandler(new FakePlatformRepository());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakePlatformRepository : IPlatformRepository
    {
        public DeletePlatformResult DeleteResult { get; init; } = DeletePlatformResult.Deleted;

        public string? PlatformId { get; private set; }

        public Task<CreatePlatformResult> CreateAsync(
            Platform platform,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UpdatePlatformResult> UpdateAsync(
            string platformId,
            string name,
            PublishSettings publishSettings,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeletePlatformResult> DeleteAsync(
            string platformId,
            CancellationToken cancellationToken)
        {
            PlatformId = platformId;

            return Task.FromResult(DeleteResult);
        }
    }
}

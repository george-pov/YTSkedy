using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class UpdatePlatformHandlerTests
{
    private static readonly YouTubeSettings Settings =
        new("main-youtube-channel", "unlisted", false);

    [Fact]
    public async Task HandleAsync_Updated_ForwardsCommandAndReturnsUpdated()
    {
        var repository = new FakePlatformRepository
        {
            UpdateResult = UpdatePlatformResult.Updated
        };
        var handler = new UpdatePlatformHandler(repository);
        var command = new UpdatePlatformCommand("p1", "Renamed channel", Settings);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.Updated, result);
        Assert.Equal("p1", repository.PlatformId);
        Assert.Equal("Renamed channel", repository.Name);
        Assert.Same(Settings, repository.PublishSettings);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNotFound()
    {
        var repository = new FakePlatformRepository
        {
            UpdateResult = UpdatePlatformResult.NotFound
        };
        var handler = new UpdatePlatformHandler(repository);
        var command = new UpdatePlatformCommand("missing", "Renamed channel", Settings);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.NotFound, result);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var repository = new FakePlatformRepository
        {
            UpdateResult = UpdatePlatformResult.NameAlreadyExists
        };
        var handler = new UpdatePlatformHandler(repository);
        var command = new UpdatePlatformCommand("p1", "Taken name", Settings);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.NameAlreadyExists, result);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new UpdatePlatformHandler(new FakePlatformRepository());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakePlatformRepository : IPlatformRepository
    {
        public UpdatePlatformResult UpdateResult { get; init; } = UpdatePlatformResult.Updated;

        public string? PlatformId { get; private set; }

        public string? Name { get; private set; }

        public PublishSettings? PublishSettings { get; private set; }

        public Task<CreatePlatformResult> CreateAsync(
            Platform platform,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UpdatePlatformResult> UpdateAsync(
            string platformId,
            string name,
            PublishSettings publishSettings,
            CancellationToken cancellationToken)
        {
            PlatformId = platformId;
            Name = name;
            PublishSettings = publishSettings;

            return Task.FromResult(UpdateResult);
        }

        public Task<DeletePlatformResult> DeleteAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

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
        var modifier = new FakePlatformModifier
        {
            UpdateResult = UpdatePlatformResult.Updated
        };
        var handler = new UpdatePlatformHandler(modifier);
        var command = new UpdatePlatformCommand("p1", "Renamed channel", Settings);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.Updated, result);
        Assert.Equal("p1", modifier.PlatformId);
        Assert.Equal("Renamed channel", modifier.Name);
        Assert.Same(Settings, modifier.PublishSettings);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNotFound()
    {
        var modifier = new FakePlatformModifier
        {
            UpdateResult = UpdatePlatformResult.NotFound
        };
        var handler = new UpdatePlatformHandler(modifier);
        var command = new UpdatePlatformCommand("missing", "Renamed channel", Settings);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.NotFound, result);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var modifier = new FakePlatformModifier
        {
            UpdateResult = UpdatePlatformResult.NameAlreadyExists
        };
        var handler = new UpdatePlatformHandler(modifier);
        var command = new UpdatePlatformCommand("p1", "Taken name", Settings);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UpdatePlatformResult.NameAlreadyExists, result);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new UpdatePlatformHandler(new FakePlatformModifier());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakePlatformModifier : IPlatformModifier
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

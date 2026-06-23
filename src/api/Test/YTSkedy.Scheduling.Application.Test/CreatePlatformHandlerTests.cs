using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class CreatePlatformHandlerTests
{
    private static readonly YouTubeSettings Settings =
        new("main-youtube-channel", "private", false);

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesPlatformAndReturnsCreatedWithId()
    {
        var repository = new FakePlatformRepository
        {
            CreateResult = CreatePlatformResult.Created("p1")
        };
        var handler = new CreatePlatformHandler(repository);
        var command = new CreatePlatformCommand("Main channel", PlatformType.YouTube, Settings);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.Created, result.Status);
        Assert.Equal("p1", result.PlatformId);

        Assert.NotNull(repository.CreatedPlatform);
        Assert.Equal("Main channel", repository.CreatedPlatform!.Name);
        Assert.Equal(PlatformType.YouTube, repository.CreatedPlatform.Type);
        Assert.Same(Settings, repository.CreatedPlatform.PublishSettings);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var repository = new FakePlatformRepository
        {
            CreateResult = CreatePlatformResult.NameAlreadyExists()
        };
        var handler = new CreatePlatformHandler(repository);
        var command = new CreatePlatformCommand("Main channel", PlatformType.YouTube, Settings);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.NameAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new CreatePlatformHandler(new FakePlatformRepository());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakePlatformRepository : IPlatformRepository
    {
        public CreatePlatformResult CreateResult { get; init; } =
            CreatePlatformResult.Created("platform-id");

        public Platform? CreatedPlatform { get; private set; }

        public Task<CreatePlatformResult> CreateAsync(
            Platform platform,
            CancellationToken cancellationToken)
        {
            CreatedPlatform = platform;

            return Task.FromResult(CreateResult);
        }

        public Task<UpdatePlatformResult> UpdateAsync(
            string platformId,
            string name,
            PublishSettings publishSettings,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeletePlatformResult> DeleteAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class CreatePlatformHandlerTests
{
    private static readonly YouTubeSettings Settings =
        new(
            new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
            "private",
            false);

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesPlatformAndReturnsCreatedWithId()
    {
        var modifier = new FakePlatformModifier
        {
            CreateResult = CreatePlatformResult.Created("p1")
        };
        var handler = new CreatePlatformHandler(modifier);
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            "main-youtube");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.Created, result.Status);
        Assert.Equal("p1", result.PlatformId);

        Assert.NotNull(modifier.CreatedPlatform);
        Assert.Equal("Main channel", modifier.CreatedPlatform!.Name);
        Assert.Equal(PlatformType.YouTube, modifier.CreatedPlatform.Type);
        Assert.Same(Settings, modifier.CreatedPlatform.PublishSettings);
        Assert.Equal("main-youtube", modifier.CreatedPlatform.ReferenceKey);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsNameAlreadyExists()
    {
        var modifier = new FakePlatformModifier
        {
            CreateResult = CreatePlatformResult.NameAlreadyExists()
        };
        var handler = new CreatePlatformHandler(modifier);
        var command = new CreatePlatformCommand("Main channel", PlatformType.YouTube, Settings);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.NameAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
    }

    [Fact]
    public async Task HandleAsync_DuplicateReferenceKey_ReturnsReferenceKeyAlreadyExists()
    {
        var modifier = new FakePlatformModifier
        {
            CreateResult = CreatePlatformResult.ReferenceKeyAlreadyExists()
        };
        var handler = new CreatePlatformHandler(modifier);
        var command = new CreatePlatformCommand(
            "Main channel",
            PlatformType.YouTube,
            Settings,
            "main-youtube");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(CreatePlatformStatus.ReferenceKeyAlreadyExists, result.Status);
        Assert.Null(result.PlatformId);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new CreatePlatformHandler(new FakePlatformModifier());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakePlatformModifier : IPlatformModifier
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
            string? referenceKey,
            PublishSettings publishSettings,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeletePlatformResult> DeleteAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class GetPlatformHandlerTests
{
    [Fact]
    public async Task HandleAsync_Found_ReturnsView()
    {
        var view = new PlatformView(
            "p1",
            "Main channel",
            PlatformType.YouTube,
            YouTubeSettings());
        var reader = new FakePlatformReader { View = view };
        var handler = new GetPlatformHandler(reader);

        var result = await handler.HandleAsync("p1", CancellationToken.None);

        Assert.Same(view, result);
        Assert.Equal("p1", reader.RequestedPlatformId);
    }

    [Fact]
    public async Task HandleAsync_Missing_ReturnsNull()
    {
        var reader = new FakePlatformReader { View = null };
        var handler = new GetPlatformHandler(reader);

        var result = await handler.HandleAsync("missing", CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_EmptyId_Throws(string? platformId)
    {
        var handler = new GetPlatformHandler(new FakePlatformReader());

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => handler.HandleAsync(platformId!, CancellationToken.None));
    }

    private sealed class FakePlatformReader : IPlatformReader
    {
        public PlatformView? View { get; init; }

        public string? RequestedPlatformId { get; private set; }

        public Task<IReadOnlyList<PlatformView>> ListAsync(
            PlatformType? type,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PlatformView?> GetAsync(
            string platformId,
            CancellationToken cancellationToken)
        {
            RequestedPlatformId = platformId;

            return Task.FromResult(View);
        }
    }

    private static YouTubeSettings YouTubeSettings() =>
        new(new YouTubeCredentials("client-id", "client-secret", "refresh-token"), "private", false);
}

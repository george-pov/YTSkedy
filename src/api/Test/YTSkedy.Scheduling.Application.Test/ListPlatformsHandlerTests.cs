using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class ListPlatformsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ForwardsTypeFilterAndReturnsViews()
    {
        var views = new[]
        {
            new PlatformView(
                "p1",
                "Main channel",
                "main-youtube",
                PlatformType.YouTube,
                YouTubeSettings(),
                RequiredPublishingContent())
        };
        var reader = new FakePlatformReader { Views = views };
        var handler = new ListPlatformsHandler(reader);

        var result = await handler.HandleAsync(
            new ListPlatformsQuery(PlatformType.YouTube),
            CancellationToken.None);

        Assert.Same(views, result);
        Assert.True(reader.ListCalled);
        Assert.Equal(PlatformType.YouTube, reader.RequestedType);
    }

    [Fact]
    public async Task HandleAsync_NoTypeFilter_PassesNullType()
    {
        var reader = new FakePlatformReader();
        var handler = new ListPlatformsHandler(reader);

        await handler.HandleAsync(new ListPlatformsQuery(null), CancellationToken.None);

        Assert.True(reader.ListCalled);
        Assert.Null(reader.RequestedType);
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        var handler = new ListPlatformsHandler(new FakePlatformReader());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakePlatformReader : IPlatformReader
    {
        public IReadOnlyList<PlatformView> Views { get; init; } = [];

        public bool ListCalled { get; private set; }

        public PlatformType? RequestedType { get; private set; }

        public Task<IReadOnlyList<PlatformView>> ListAsync(
            PlatformType? type,
            CancellationToken cancellationToken)
        {
            ListCalled = true;
            RequestedType = type;

            return Task.FromResult(Views);
        }

        public Task<PlatformView?> GetAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private static YouTubeSettings YouTubeSettings() =>
        new(new YouTubeCredentials("client-id", "client-secret", "refresh-token"), "private", false);

    private static PublishingContent RequiredPublishingContent() =>
        new("title-template", "description-template");
}

using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class ThumbnailPublisherSelectorTests
{
    [Fact]
    public void Find_RegisteredType_ReturnsPublisher()
    {
        var youtube = new FakeThumbnailPublisher(PlatformType.YouTube);
        var selector = new ThumbnailPublisherSelector([youtube]);

        Assert.Same(youtube, selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Find_UnregisteredType_ReturnsNull()
    {
        var selector = new ThumbnailPublisherSelector([]);

        Assert.Null(selector.Find(PlatformType.WordPress));
    }

    [Fact]
    public void Constructor_DuplicateType_UsesFirstPublisher()
    {
        var first = new FakeThumbnailPublisher(PlatformType.YouTube);
        var second = new FakeThumbnailPublisher(PlatformType.YouTube);
        var selector = new ThumbnailPublisherSelector([first, second]);

        Assert.Same(first, selector.Find(PlatformType.YouTube));
    }

    private sealed class FakeThumbnailPublisher(
        PlatformType type) : IThumbnailPublisher
    {
        public PlatformType Type { get; } = type;

        public Task PublishAsync(
            ThumbnailPublishRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

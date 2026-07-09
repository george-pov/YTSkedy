using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class PlatformPublisherSelectorTests
{
    [Fact]
    public void Find_RegisteredType_ReturnsThatPublisher()
    {
        var youTube = new FakePlatformPublisher(PlatformType.YouTube);
        var selector = new PlatformPublisherSelector([youTube]);

        Assert.Same(youTube, selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Find_WordPressPublisherSupplied_ReturnsWordPressPublisher()
    {
        var wordPress = new FakePlatformPublisher(PlatformType.WordPress);
        var selector = new PlatformPublisherSelector(
            [new FakePlatformPublisher(PlatformType.YouTube), wordPress]);

        Assert.Same(wordPress, selector.Find(PlatformType.WordPress));
    }

    [Fact]
    public void Find_UnregisteredType_ReturnsNull()
    {
        var selector = new PlatformPublisherSelector([new FakePlatformPublisher(PlatformType.YouTube)]);

        Assert.Null(selector.Find(PlatformType.WordPress));
    }

    [Fact]
    public void Find_NoPublishers_ReturnsNull()
    {
        var selector = new PlatformPublisherSelector([]);

        Assert.Null(selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Constructor_NullPublishers_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PlatformPublisherSelector(null!));
    }

    private sealed class FakePlatformPublisher(PlatformType type) : IPlatformPublisher
    {
        public PlatformType Type => type;

        public Task<PlatformPublishResult> PublishAsync(
            PlatformPublishRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

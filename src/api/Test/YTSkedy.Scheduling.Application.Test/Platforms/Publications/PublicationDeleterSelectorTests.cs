using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class PublicationDeleterSelectorTests
{
    [Fact]
    public void Find_RegisteredType_ReturnsThatDeleter()
    {
        var youTube = new FakePublicationDeleter(PlatformType.YouTube);
        var selector = new PublicationDeleterSelector([youTube]);

        Assert.Same(youTube, selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Find_UnregisteredType_ReturnsNull()
    {
        var selector = new PublicationDeleterSelector(
            [new FakePublicationDeleter(PlatformType.YouTube)]);

        Assert.Null(selector.Find(PlatformType.WordPress));
    }

    [Fact]
    public void Find_NoDeleters_ReturnsNull()
    {
        var selector = new PublicationDeleterSelector([]);

        Assert.Null(selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Constructor_NullDeleters_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PublicationDeleterSelector(null!));
    }

    private sealed class FakePublicationDeleter(PlatformType type) : IPlatformPublicationDeleter
    {
        public PlatformType Type => type;

        public Task<PublicationDeleteResult> DeleteAsync(
            PublicationDeleteRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

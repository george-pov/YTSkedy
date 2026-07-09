using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class PlatformTypeAdapterSelectorTests
{
    [Fact]
    public void Find_RegisteredType_ReturnsThatAdapter()
    {
        var youTube = new FakeAdapter(PlatformType.YouTube);
        var selector = new PlatformTypeAdapterSelector<FakeAdapter>([youTube]);

        Assert.Same(youTube, selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Find_WordPressAdapterSupplied_ReturnsWordPressAdapter()
    {
        var wordPress = new FakeAdapter(PlatformType.WordPress);
        var selector = new PlatformTypeAdapterSelector<FakeAdapter>(
            [new FakeAdapter(PlatformType.YouTube), wordPress]);

        Assert.Same(wordPress, selector.Find(PlatformType.WordPress));
    }

    [Fact]
    public void Find_UnregisteredType_ReturnsNull()
    {
        var selector = new PlatformTypeAdapterSelector<FakeAdapter>(
            [new FakeAdapter(PlatformType.YouTube)]);

        Assert.Null(selector.Find(PlatformType.WordPress));
    }

    [Fact]
    public void Find_NoAdapters_ReturnsNull()
    {
        var selector = new PlatformTypeAdapterSelector<FakeAdapter>([]);

        Assert.Null(selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Constructor_DuplicateType_UsesFirstAdapter()
    {
        var first = new FakeAdapter(PlatformType.YouTube);
        var second = new FakeAdapter(PlatformType.YouTube);
        var selector = new PlatformTypeAdapterSelector<FakeAdapter>([first, second]);

        Assert.Same(first, selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Constructor_NullAdapters_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PlatformTypeAdapterSelector<FakeAdapter>(null!));
    }

    private sealed class FakeAdapter(PlatformType type) : IPlatformTypeAdapter
    {
        public PlatformType Type => type;
    }
}

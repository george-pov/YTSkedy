using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class PlatformTypeAdapterSelectorTests
{
    [Fact]
    public void Find_RegisteredType_ReturnsThatAdapter()
    {
        var youTube = Adapter(PlatformType.YouTube);
        var selector = new PlatformTypeAdapterSelector<IPlatformTypeAdapter>([youTube.Object]);

        Assert.Same(youTube.Object, selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Find_WordPressAdapterSupplied_ReturnsWordPressAdapter()
    {
        var wordPress = Adapter(PlatformType.WordPress);
        var selector = new PlatformTypeAdapterSelector<IPlatformTypeAdapter>(
            [Adapter(PlatformType.YouTube).Object, wordPress.Object]);

        Assert.Same(wordPress.Object, selector.Find(PlatformType.WordPress));
    }

    [Fact]
    public void Find_UnregisteredType_ReturnsNull()
    {
        var selector = new PlatformTypeAdapterSelector<IPlatformTypeAdapter>(
            [Adapter(PlatformType.YouTube).Object]);

        Assert.Null(selector.Find(PlatformType.WordPress));
    }

    [Fact]
    public void Find_NoAdapters_ReturnsNull()
    {
        var selector = new PlatformTypeAdapterSelector<IPlatformTypeAdapter>([]);

        Assert.Null(selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Constructor_DuplicateType_UsesFirstAdapter()
    {
        var first = Adapter(PlatformType.YouTube);
        var second = Adapter(PlatformType.YouTube);
        var selector = new PlatformTypeAdapterSelector<IPlatformTypeAdapter>(
            [first.Object, second.Object]);

        Assert.Same(first.Object, selector.Find(PlatformType.YouTube));
    }

    [Fact]
    public void Constructor_NullAdapters_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PlatformTypeAdapterSelector<IPlatformTypeAdapter>(null!));
    }

    private static Mock<IPlatformTypeAdapter> Adapter(PlatformType type)
    {
        var adapter = new Mock<IPlatformTypeAdapter>();
        adapter.SetupGet(candidate => candidate.Type).Returns(type);
        return adapter;
    }
}

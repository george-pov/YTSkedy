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
            ApplicationTestData.Platform(
                platformId: "p1",
                name: "Main channel",
                referenceKey: "main-youtube")
        };
        var reader = new FakePlatformReader(platforms: views);
        var handler = new ListPlatformsHandler(reader);

        var result = await handler.HandleAsync(
            new ListPlatformsQuery(PlatformType.YouTube),
            CancellationToken.None);

        Assert.Equal(views, result);
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
}

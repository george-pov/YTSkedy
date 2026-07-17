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
        var reader = new Mock<IPlatformReader>();
        reader
            .Setup(candidate => candidate.ListAsync(
                PlatformType.YouTube,
                CancellationToken.None))
            .ReturnsAsync(views);
        var handler = new ListPlatformsHandler(reader.Object);

        var result = await handler.HandleAsync(
            new ListPlatformsQuery(PlatformType.YouTube),
            CancellationToken.None);

        Assert.Equal(views, result);
        reader.Verify(candidate => candidate.ListAsync(
            PlatformType.YouTube,
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NoTypeFilter_PassesNullType()
    {
        var reader = new Mock<IPlatformReader>();
        reader
            .Setup(candidate => candidate.ListAsync(null, CancellationToken.None))
            .ReturnsAsync([]);
        var handler = new ListPlatformsHandler(reader.Object);

        await handler.HandleAsync(new ListPlatformsQuery(null), CancellationToken.None);

        reader.Verify(candidate => candidate.ListAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        var handler = new ListPlatformsHandler(new Mock<IPlatformReader>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }
}

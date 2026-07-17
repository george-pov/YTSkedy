using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class ListPlatformsHandlerTests
{
    private readonly Mock<IPlatformReader> _reader = new();
    private readonly ListPlatformsHandler _handler;

    public ListPlatformsHandlerTests()
    {
        _handler = new ListPlatformsHandler(_reader.Object);
    }

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
        _reader
            .Setup(candidate => candidate.ListAsync(
                PlatformType.YouTube,
                CancellationToken.None))
            .ReturnsAsync(views);
        var result = await _handler.HandleAsync(
            new ListPlatformsQuery(PlatformType.YouTube),
            CancellationToken.None);

        Assert.Equal(views, result);
        _reader.Verify(candidate => candidate.ListAsync(
            PlatformType.YouTube,
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NoTypeFilter_PassesNullType()
    {
        _reader
            .Setup(candidate => candidate.ListAsync(null, CancellationToken.None))
            .ReturnsAsync([]);
        await _handler.HandleAsync(new ListPlatformsQuery(null), CancellationToken.None);

        _reader.Verify(candidate => candidate.ListAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));
    }
}

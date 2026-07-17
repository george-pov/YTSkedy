using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class GetPlatformHandlerTests
{
    private readonly Mock<IPlatformReader> _reader = new();
    private readonly GetPlatformHandler _handler;

    public GetPlatformHandlerTests()
    {
        _handler = new GetPlatformHandler(_reader.Object);
    }

    [Fact]
    public async Task HandleAsync_Found_ReturnsView()
    {
        var view = ApplicationTestData.Platform(
            platformId: "p1",
            name: "Main channel",
            referenceKey: "main-youtube");
        _reader
            .Setup(candidate => candidate.GetAsync("p1", CancellationToken.None))
            .ReturnsAsync(view);
        var result = await _handler.HandleAsync("p1", CancellationToken.None);

        Assert.Same(view, result);
        _reader.Verify(candidate => candidate.GetAsync("p1", CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_Missing_ReturnsNull()
    {
        _reader
            .Setup(candidate => candidate.GetAsync("missing", CancellationToken.None))
            .ReturnsAsync((PlatformView?)null);
        var result = await _handler.HandleAsync("missing", CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_EmptyId_Throws(string? platformId)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _handler.HandleAsync(platformId!, CancellationToken.None));
    }
}

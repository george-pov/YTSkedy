using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class GetPlatformHandlerTests
{
    [Fact]
    public async Task HandleAsync_Found_ReturnsView()
    {
        var view = ApplicationTestData.Platform(
            platformId: "p1",
            name: "Main channel",
            referenceKey: "main-youtube");
        var reader = new Mock<IPlatformReader>();
        reader
            .Setup(candidate => candidate.GetAsync("p1", CancellationToken.None))
            .ReturnsAsync(view);
        var handler = new GetPlatformHandler(reader.Object);

        var result = await handler.HandleAsync("p1", CancellationToken.None);

        Assert.Same(view, result);
        reader.Verify(candidate => candidate.GetAsync("p1", CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_Missing_ReturnsNull()
    {
        var reader = new Mock<IPlatformReader>();
        reader
            .Setup(candidate => candidate.GetAsync("missing", CancellationToken.None))
            .ReturnsAsync((PlatformView?)null);
        var handler = new GetPlatformHandler(reader.Object);

        var result = await handler.HandleAsync("missing", CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_EmptyId_Throws(string? platformId)
    {
        var handler = new GetPlatformHandler(new Mock<IPlatformReader>().Object);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => handler.HandleAsync(platformId!, CancellationToken.None));
    }
}

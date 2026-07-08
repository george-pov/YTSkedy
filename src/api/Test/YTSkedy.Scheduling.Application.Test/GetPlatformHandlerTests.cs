using YTSkedy.Scheduling.Application.Platforms;

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
        var reader = new FakePlatformReader(getResult: view);
        var handler = new GetPlatformHandler(reader);

        var result = await handler.HandleAsync("p1", CancellationToken.None);

        Assert.Same(view, result);
        Assert.Equal("p1", reader.PlatformId);
    }

    [Fact]
    public async Task HandleAsync_Missing_ReturnsNull()
    {
        var reader = new FakePlatformReader(getResult: null);
        var handler = new GetPlatformHandler(reader);

        var result = await handler.HandleAsync("missing", CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_EmptyId_Throws(string? platformId)
    {
        var handler = new GetPlatformHandler(new FakePlatformReader());

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => handler.HandleAsync(platformId!, CancellationToken.None));
    }
}

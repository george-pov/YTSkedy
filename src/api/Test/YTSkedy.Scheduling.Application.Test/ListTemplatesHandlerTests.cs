using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class ListTemplatesHandlerTests
{
    private readonly Mock<ITemplateReader> _reader = new();
    private readonly ListTemplatesHandler _handler;

    public ListTemplatesHandlerTests()
    {
        _handler = new ListTemplatesHandler(_reader.Object);
    }

    [Fact]
    public async Task HandleAsync_NoType_ForwardsNullAndReturnsViews()
    {
        var views = new[]
        {
            new TemplateView("id1", "First", TemplateType.YouTube, "content one"),
            new TemplateView("id2", "Second", TemplateType.WordPress, "content two")
        };
        _reader
            .Setup(candidate => candidate.ListAsync(null, CancellationToken.None))
            .ReturnsAsync(views);
        var result = await _handler.HandleAsync(
            new ListTemplatesQuery(null),
            CancellationToken.None);

        Assert.Equal(views, result);
        _reader.Verify(candidate => candidate.ListAsync(null, CancellationToken.None));
    }

    [Theory]
    [InlineData(TemplateType.YouTube)]
    [InlineData(TemplateType.WordPress)]
    public async Task HandleAsync_WithType_ForwardsTypeToReader(TemplateType type)
    {
        _reader
            .Setup(candidate => candidate.ListAsync(type, CancellationToken.None))
            .ReturnsAsync([]);
        await _handler.HandleAsync(new ListTemplatesQuery(type), CancellationToken.None);

        _reader.Verify(candidate => candidate.ListAsync(type, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));
    }
}

using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class ListTemplatesHandlerTests
{
    [Fact]
    public async Task HandleAsync_NoType_ForwardsNullAndReturnsViews()
    {
        var views = new[]
        {
            new TemplateView("id1", "First", TemplateType.YouTube, "content one"),
            new TemplateView("id2", "Second", TemplateType.WordPress, "content two")
        };
        var reader = new Mock<ITemplateReader>();
        reader
            .Setup(candidate => candidate.ListAsync(null, CancellationToken.None))
            .ReturnsAsync(views);
        var handler = new ListTemplatesHandler(reader.Object);

        var result = await handler.HandleAsync(
            new ListTemplatesQuery(null),
            CancellationToken.None);

        Assert.Equal(views, result);
        reader.Verify(candidate => candidate.ListAsync(null, CancellationToken.None));
    }

    [Theory]
    [InlineData(TemplateType.YouTube)]
    [InlineData(TemplateType.WordPress)]
    public async Task HandleAsync_WithType_ForwardsTypeToReader(TemplateType type)
    {
        var reader = new Mock<ITemplateReader>();
        reader
            .Setup(candidate => candidate.ListAsync(type, CancellationToken.None))
            .ReturnsAsync([]);
        var handler = new ListTemplatesHandler(reader.Object);

        await handler.HandleAsync(new ListTemplatesQuery(type), CancellationToken.None);

        reader.Verify(candidate => candidate.ListAsync(type, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        var handler = new ListTemplatesHandler(new Mock<ITemplateReader>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }
}

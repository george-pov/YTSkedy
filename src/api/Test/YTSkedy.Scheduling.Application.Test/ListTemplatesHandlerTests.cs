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
        var reader = new FakeTemplateReader(views);
        var handler = new ListTemplatesHandler(reader);

        var result = await handler.HandleAsync(
            new ListTemplatesQuery(null),
            CancellationToken.None);

        Assert.Equal(views, result);
        Assert.Equal(1, reader.ListCallCount);
        Assert.Null(reader.RequestedType);
    }

    [Theory]
    [InlineData(TemplateType.YouTube)]
    [InlineData(TemplateType.WordPress)]
    public async Task HandleAsync_WithType_ForwardsTypeToReader(TemplateType type)
    {
        var reader = new FakeTemplateReader([]);
        var handler = new ListTemplatesHandler(reader);

        await handler.HandleAsync(new ListTemplatesQuery(type), CancellationToken.None);

        Assert.Equal(1, reader.ListCallCount);
        Assert.Equal(type, reader.RequestedType);
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        var handler = new ListTemplatesHandler(new FakeTemplateReader([]));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private sealed class FakeTemplateReader(IReadOnlyList<TemplateView> views) : ITemplateReader
    {
        public int ListCallCount { get; private set; }
        public TemplateType? RequestedType { get; private set; }

        public Task<TemplateView?> GetAsync(
            TemplateType type,
            string templateId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TemplateView>> ListAsync(
            TemplateType? type,
            CancellationToken cancellationToken)
        {
            ListCallCount++;
            RequestedType = type;

            return Task.FromResult(views);
        }
    }
}

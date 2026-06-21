using YTSkedy.Infrastructure.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Infrastructure.Test.Templates;

public class TemplateReadMapperTests
{
    [Fact]
    public void ToView_Entity_MapsTemplateFields()
    {
        var entity = CreateEntity(
            "9f8b1c2d3e4f",
            "Weeknight stream",
            "YouTube",
            "Live at {{ localizedTime }}");

        var view = TemplateReadMapper.ToView(entity);

        Assert.Equal("9f8b1c2d3e4f", view.Id);
        Assert.Equal("Weeknight stream", view.Name);
        Assert.Equal(TemplateType.YouTube, view.Type);
        Assert.Equal("Live at {{ localizedTime }}", view.Content);
    }

    [Fact]
    public void ToView_WordPressEntity_MapsType()
    {
        var entity = CreateEntity("id1", "Blog post", "WordPress", "content");

        var view = TemplateReadMapper.ToView(entity);

        Assert.Equal(TemplateType.WordPress, view.Type);
    }

    [Fact]
    public void ToViews_Entities_MapsEachInInputOrder()
    {
        var entities = new[]
        {
            CreateEntity("id1", "First", "YouTube", "content one"),
            CreateEntity("id2", "Second", "WordPress", "content two")
        };

        var views = TemplateReadMapper.ToViews(entities);

        Assert.Equal(["id1", "id2"], views.Select(view => view.Id));
        Assert.Equal(
            [TemplateType.YouTube, TemplateType.WordPress],
            views.Select(view => view.Type));
    }

    [Theory]
    [InlineData("YouTube", TemplateType.YouTube)]
    [InlineData("youtube", TemplateType.YouTube)]
    [InlineData("WordPress", TemplateType.WordPress)]
    [InlineData("wordpress", TemplateType.WordPress)]
    public void ParseType_KnownType_ReturnsMatchingType(string stored, TemplateType expected)
    {
        Assert.Equal(expected, TemplateReadMapper.ParseType(stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-real-type")]
    public void ParseType_UnknownType_DefaultsToYouTube(string? stored)
    {
        Assert.Equal(TemplateType.YouTube, TemplateReadMapper.ParseType(stored));
    }

    private static TemplateEntity CreateEntity(
        string id,
        string name,
        string type,
        string content) =>
        new()
        {
            PartitionKey = string.Equals(type, "WordPress", StringComparison.Ordinal)
                ? "templates-wordpress"
                : "templates-youtube",
            RowKey = id,
            TemplateId = id,
            Name = name,
            Type = type,
            Content = content,
            CreatedUtc = new DateTimeOffset(2026, 06, 15, 17, 00, 00, TimeSpan.Zero)
        };
}

using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.Platforms;

public class PlatformViewMapperTests
{
    [Fact]
    public void ToView_Entity_MapsPlatformFields()
    {
        var entity = CreateEntity("YouTube");

        var view = PlatformViewMapper.ToView(entity);

        Assert.Equal("platform-1", view.PlatformId);
        Assert.Equal("Main YouTube channel", view.Name);
        Assert.Null(view.ReferenceKey);
        Assert.Equal(PlatformType.YouTube, view.Type);
        Assert.IsType<YouTubeSettings>(view.PublishSettings);
    }

    [Fact]
    public void ToView_EntityWithReferenceKey_MapsDisplayValue()
    {
        var entity = CreateEntity("YouTube");
        entity.ReferenceKey = "youTube1";

        var view = PlatformViewMapper.ToView(entity);

        Assert.Equal("youTube1", view.ReferenceKey);
    }

    [Fact]
    public void ToView_EntityWithoutReferenceKey_MapsNull()
    {
        var entity = CreateEntity("YouTube");

        var view = PlatformViewMapper.ToView(entity);

        Assert.Null(view.ReferenceKey);
    }

    [Fact]
    public void ToView_WordPressEntity_MapsType()
    {
        var entity = CreateEntity(
            "WordPress",
            PublishSettingsSerializer.Serialize(
                PlatformType.WordPress,
                new WordPressSettings(
                    "https://example.com",
                    "editor",
                    "application-password",
                    "publish")));

        var view = PlatformViewMapper.ToView(entity);

        Assert.Equal(PlatformType.WordPress, view.Type);
        Assert.IsType<WordPressSettings>(view.PublishSettings);
    }

    [Theory]
    [InlineData("YouTube", PlatformType.YouTube)]
    [InlineData("youtube", PlatformType.YouTube)]
    [InlineData("WordPress", PlatformType.WordPress)]
    [InlineData("wordpress", PlatformType.WordPress)]
    public void ParseType_KnownType_ReturnsMatchingType(string stored, PlatformType expected)
    {
        Assert.Equal(expected, PlatformViewMapper.ParseType(stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-real-type")]
    [InlineData("0")]
    public void ParseType_UnknownType_Throws(string? stored)
    {
        Assert.Throws<InvalidOperationException>(() => PlatformViewMapper.ParseType(stored));
    }

    private static PlatformEntity CreateEntity(
        string type,
        string? publishSettingsJson = null) =>
        new()
        {
            PartitionKey = "platforms",
            RowKey = "platform-platform-1",
            PlatformId = "platform-1",
            Name = "Main YouTube channel",
            Type = type,
            PublishSettingsJson = publishSettingsJson ??
                PublishSettingsSerializer.Serialize(
                    PlatformType.YouTube,
                    new YouTubeSettings(
                        new YouTubeCredentials(
                            "client-id",
                            "client-secret",
                            "refresh-token"),
                        "private",
                        false)),
            CreatedUtc = new DateTimeOffset(2026, 06, 15, 17, 00, 00, TimeSpan.Zero),
            UpdatedUtc = new DateTimeOffset(2026, 06, 15, 17, 00, 00, TimeSpan.Zero)
        };
}

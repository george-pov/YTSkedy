using Google.Apis.YouTube.v3.Data;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubeVideoUpdateFactoryTests
{
    [Theory]
    [InlineData("private", null, false, null)]
    [InlineData("private", "27", false, "snippet")]
    [InlineData("private", null, true, "status")]
    [InlineData("unlisted", null, false, "status")]
    [InlineData("public", "27", false, "snippet,status")]
    public void RequiredParts_Settings_ReturnsMinimumParts(
        string privacyStatus,
        string? categoryId,
        bool containsSyntheticMedia,
        string? expected)
    {
        var settings = Settings(privacyStatus, categoryId, containsSyntheticMedia);

        var parts = YouTubeVideoUpdateFactory.RequiredParts(settings);

        Assert.Equal(expected, parts.ApiValue);
    }

    [Fact]
    public void Create_SnippetOnly_PreservesMutableSnippetValues()
    {
        var current = CurrentVideo();

        var update = YouTubeVideoUpdateFactory.Create(
            current,
            Settings(categoryId: "27"),
            new YouTubeVideoUpdateParts(IncludeSnippet: true, IncludeStatus: false));

        Assert.Equal("snippet", update.Parts);
        Assert.Equal(current.Id, update.Video.Id);
        Assert.Equal("27", update.Video.Snippet.CategoryId);
        Assert.Equal(current.Snippet.Title, update.Video.Snippet.Title);
        Assert.Equal(current.Snippet.Description, update.Video.Snippet.Description);
        Assert.Equal(current.Snippet.DefaultLanguage, update.Video.Snippet.DefaultLanguage);
        Assert.Equal(current.Snippet.Tags, update.Video.Snippet.Tags);
        Assert.Null(update.Video.Status);
    }

    [Fact]
    public void Create_StatusOnly_PreservesMutableStatusValuesAndAppliesExplicitDisclosure()
    {
        var current = CurrentVideo();

        var update = YouTubeVideoUpdateFactory.Create(
            current,
            Settings("private", containsSyntheticMedia: false),
            new YouTubeVideoUpdateParts(IncludeSnippet: false, IncludeStatus: true));

        Assert.Equal("status", update.Parts);
        Assert.Null(update.Video.Snippet);
        Assert.Equal(current.Status.Embeddable, update.Video.Status.Embeddable);
        Assert.Equal(current.Status.License, update.Video.Status.License);
        Assert.Equal(current.Status.PublicStatsViewable, update.Video.Status.PublicStatsViewable);
        Assert.Equal(
            current.Status.PublishAtDateTimeOffset,
            update.Video.Status.PublishAtDateTimeOffset);
        Assert.Equal("private", update.Video.Status.PrivacyStatus);
        Assert.False(update.Video.Status.ContainsSyntheticMedia);
        Assert.False(update.Video.Status.SelfDeclaredMadeForKids);
    }

    private static YouTubeSettings Settings(
        string privacyStatus = "private",
        string? categoryId = null,
        bool containsSyntheticMedia = false) =>
        new(
            new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
            privacyStatus,
            false,
            categoryId,
            containsSyntheticMedia);

    private static Video CurrentVideo() =>
        new()
        {
            Id = "broadcast-id",
            Snippet = new VideoSnippet
            {
                CategoryId = "22",
                DefaultLanguage = "en",
                Description = "Description",
                Tags = ["one", "two"],
                Title = "Title"
            },
            Status = new VideoStatus
            {
                Embeddable = true,
                License = "youtube",
                PrivacyStatus = "private",
                PublicStatsViewable = true,
                PublishAtDateTimeOffset =
                    new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero),
                SelfDeclaredMadeForKids = false
            }
        };
}

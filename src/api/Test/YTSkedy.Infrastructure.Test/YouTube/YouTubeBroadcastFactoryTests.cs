using Google.Apis.YouTube.v3.Data;
using YTSkedy.Infrastructure.YouTube;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubeBroadcastFactoryTests
{
    [Fact]
    public void Create_MapsContentAndStatus()
    {
        var start = new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero);

        var broadcast = YouTubeBroadcastFactory.Create(
            "Stream title",
            "Stream description",
            start,
            "unlisted",
            selfDeclaredMadeForKids: true);

        Assert.Equal("Stream title", broadcast.Snippet.Title);
        Assert.Equal("Stream description", broadcast.Snippet.Description);
        Assert.Equal(start, broadcast.Snippet.ScheduledStartTimeDateTimeOffset);
        Assert.Equal("unlisted", broadcast.Status.PrivacyStatus);
        Assert.True(broadcast.Status.SelfDeclaredMadeForKids);
    }

    [Fact]
    public void Create_NullDescription_BecomesEmptyString()
    {
        var broadcast = YouTubeBroadcastFactory.Create(
            "Title",
            description: null,
            new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero),
            "private",
            selfDeclaredMadeForKids: false);

        Assert.Equal(string.Empty, broadcast.Snippet.Description);
    }

    [Fact]
    public void Create_PrivateNotForKids_MapsStatus()
    {
        var broadcast = YouTubeBroadcastFactory.Create(
            "Title",
            "Description",
            new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero),
            "private",
            selfDeclaredMadeForKids: false);

        Assert.Equal("private", broadcast.Status.PrivacyStatus);
        Assert.False(broadcast.Status.SelfDeclaredMadeForKids);
    }
}

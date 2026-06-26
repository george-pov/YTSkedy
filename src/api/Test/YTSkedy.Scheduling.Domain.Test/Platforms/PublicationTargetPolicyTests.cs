using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PublicationTargetPolicyTests
{
    [Fact]
    public void Matches_YouTubeClientIdMatches_ReturnsTrue()
    {
        var platform = YouTubePlatform("client-id");
        var snapshot = new PublicationTargetSnapshot(
            PlatformType.YouTube,
            WordPressSiteUrl: null,
            YouTubeClientId: "client-id");

        Assert.True(PublicationTargetPolicy.Matches(platform, snapshot));
    }

    [Fact]
    public void Matches_YouTubeClientIdChanged_ReturnsFalse()
    {
        var platform = YouTubePlatform("client-id");
        var snapshot = new PublicationTargetSnapshot(
            PlatformType.YouTube,
            WordPressSiteUrl: null,
            YouTubeClientId: "other-client-id");

        Assert.False(PublicationTargetPolicy.Matches(platform, snapshot));
    }

    [Fact]
    public void Matches_WordPressSiteUrlMatches_ReturnsTrue()
    {
        var platform = new PlatformView(
            "p1",
            "Company blog",
            PlatformType.WordPress,
            new WordPressSettings(
                "https://example.com/",
                "editor",
                "application-password",
                "publish"));
        var snapshot = new PublicationTargetSnapshot(
            PlatformType.WordPress,
            WordPressSiteUrl: "https://example.com",
            YouTubeClientId: null);

        Assert.True(PublicationTargetPolicy.Matches(platform, snapshot));
    }

    [Fact]
    public void Matches_TypeChanged_ReturnsFalse()
    {
        var platform = YouTubePlatform("client-id");
        var snapshot = new PublicationTargetSnapshot(
            PlatformType.WordPress,
            WordPressSiteUrl: "https://example.com",
            YouTubeClientId: null);

        Assert.False(PublicationTargetPolicy.Matches(platform, snapshot));
    }

    private static PlatformView YouTubePlatform(string clientId) =>
        new(
            "p1",
            "Main YouTube channel",
            PlatformType.YouTube,
            new YouTubeSettings(
                new YouTubeCredentials(clientId, "client-secret", "refresh-token"),
                "private",
                false));
}

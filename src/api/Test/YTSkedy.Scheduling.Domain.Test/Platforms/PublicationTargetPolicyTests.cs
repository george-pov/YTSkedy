using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PublicationTargetPolicyTests
{
    [Fact]
    public void Matches_YouTubeClientIdMatches_ReturnsTrue()
    {
        var platform = PlatformSamples.PlatformView(
            publishSettings: PlatformSamples.YouTubeSettings(clientId: "client-id"));
        var snapshot = new PublicationTargetSnapshot(
            PlatformType.YouTube,
            WordPressSiteUrl: null,
            YouTubeClientId: "client-id");

        Assert.True(PublicationTargetPolicy.Matches(platform, snapshot));
    }

    [Fact]
    public void Matches_YouTubeClientIdChanged_ReturnsFalse()
    {
        var platform = PlatformSamples.PlatformView(
            publishSettings: PlatformSamples.YouTubeSettings(clientId: "client-id"));
        var snapshot = new PublicationTargetSnapshot(
            PlatformType.YouTube,
            WordPressSiteUrl: null,
            YouTubeClientId: "other-client-id");

        Assert.False(PublicationTargetPolicy.Matches(platform, snapshot));
    }

    [Fact]
    public void Matches_WordPressSiteUrlMatches_ReturnsTrue()
    {
        var platform = PlatformSamples.PlatformView(
            name: "Company blog",
            type: PlatformType.WordPress,
            publishSettings: PlatformSamples.WordPressSettings());
        var snapshot = new PublicationTargetSnapshot(
            PlatformType.WordPress,
            WordPressSiteUrl: "https://example.com",
            YouTubeClientId: null);

        Assert.True(PublicationTargetPolicy.Matches(platform, snapshot));
    }

    [Fact]
    public void Matches_TypeChanged_ReturnsFalse()
    {
        var platform = PlatformSamples.PlatformView(
            publishSettings: PlatformSamples.YouTubeSettings(clientId: "client-id"));
        var snapshot = new PublicationTargetSnapshot(
            PlatformType.WordPress,
            WordPressSiteUrl: "https://example.com",
            YouTubeClientId: null);

        Assert.False(PublicationTargetPolicy.Matches(platform, snapshot));
    }
}

using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Test.Platforms;

public class PublicationTargetPolicyTests
{
    public static TheoryData<MatchesCase> MatchesCases => new()
    {
        new(
            "YouTubeClientIdMatches",
            PlatformSamples.PlatformView(
                publishSettings: PlatformSamples.YouTubeSettings(clientId: "client-id")),
            new PublicationTargetSnapshot(
                PlatformType.YouTube,
                WordPressSiteUrl: null,
                YouTubeClientId: "client-id"),
            Expected: true),
        new(
            "YouTubeClientIdChanged",
            PlatformSamples.PlatformView(
                publishSettings: PlatformSamples.YouTubeSettings(clientId: "client-id")),
            new PublicationTargetSnapshot(
                PlatformType.YouTube,
                WordPressSiteUrl: null,
                YouTubeClientId: "other-client-id"),
            Expected: false),
        new(
            "WordPressSiteUrlMatches",
            PlatformSamples.PlatformView(
                name: "Company blog",
                type: PlatformType.WordPress,
                publishSettings: PlatformSamples.WordPressSettings()),
            new PublicationTargetSnapshot(
                PlatformType.WordPress,
                WordPressSiteUrl: "https://example.com",
                YouTubeClientId: null),
            Expected: true),
        new(
            "TypeChanged",
            PlatformSamples.PlatformView(
                publishSettings: PlatformSamples.YouTubeSettings(clientId: "client-id")),
            new PublicationTargetSnapshot(
                PlatformType.WordPress,
                WordPressSiteUrl: "https://example.com",
                YouTubeClientId: null),
            Expected: false),
        new(
            "NullSnapshot",
            PlatformSamples.PlatformView(),
            Snapshot: null,
            Expected: false),
        new(
            "WordPressSiteUrlChanged",
            PlatformSamples.PlatformView(
                name: "Company blog",
                type: PlatformType.WordPress,
                publishSettings: PlatformSamples.WordPressSettings()),
            new PublicationTargetSnapshot(
                PlatformType.WordPress,
                WordPressSiteUrl: "https://other.example.com",
                YouTubeClientId: null),
            Expected: false),
        new(
            "BlankProviderIdentity",
            PlatformSamples.PlatformView(
                publishSettings: PlatformSamples.YouTubeSettings(clientId: "client-id")),
            new PublicationTargetSnapshot(
                PlatformType.YouTube,
                WordPressSiteUrl: null,
                YouTubeClientId: " "),
            Expected: false)
    };

    [Theory]
    [MemberData(nameof(MatchesCases))]
    public void Matches_State_ReturnsExpected(MatchesCase scenario)
    {
        var actual = PublicationTargetPolicy.Matches(scenario.Platform, scenario.Snapshot);

        Assert.Equal(scenario.Expected, actual);
    }

    public sealed record MatchesCase(
        string Name,
        PlatformView Platform,
        PublicationTargetSnapshot? Snapshot,
        bool Expected)
    {
        public override string ToString() => Name;
    }
}

using Microsoft.Extensions.Options;
using YTSkedy.Infrastructure.YouTube;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubeCredentialStoreTests
{
    [Fact]
    public void Find_ConfiguredReference_ReturnsCredentials()
    {
        var store = CreateStore(("main-youtube-channel", Complete()));

        var credentials = store.Find("main-youtube-channel");

        Assert.NotNull(credentials);
        Assert.Equal("client-id", credentials!.ClientId);
        Assert.Equal("client-secret", credentials.ClientSecret);
        Assert.Equal("refresh-token", credentials.RefreshToken);
    }

    [Fact]
    public void Find_ReferenceMatchIsCaseInsensitive()
    {
        var store = CreateStore(("Main-YouTube-Channel", Complete()));

        Assert.NotNull(store.Find("main-youtube-channel"));
    }

    [Fact]
    public void Find_UnknownReference_ReturnsNull()
    {
        var store = CreateStore(("main-youtube-channel", Complete()));

        Assert.Null(store.Find("other-channel"));
    }

    [Theory]
    [InlineData("", "client-secret", "refresh-token")]
    [InlineData("client-id", "", "refresh-token")]
    [InlineData("client-id", "client-secret", "")]
    public void Find_IncompleteCredentials_ReturnsNull(
        string clientId,
        string clientSecret,
        string refreshToken)
    {
        var store = CreateStore(("main-youtube-channel", new YouTubeCredentials
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            RefreshToken = refreshToken
        }));

        Assert.Null(store.Find("main-youtube-channel"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Find_BlankReference_ReturnsNull(string reference)
    {
        var store = CreateStore(("main-youtube-channel", Complete()));

        Assert.Null(store.Find(reference));
    }

    private static YouTubeCredentials Complete() =>
        new()
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RefreshToken = "refresh-token"
        };

    private static YouTubeCredentialStore CreateStore(
        params (string Key, YouTubeCredentials Credentials)[] entries)
    {
        var options = new YouTubeOptions();
        foreach (var (key, credentials) in entries)
        {
            options[key] = credentials;
        }

        return new YouTubeCredentialStore(Options.Create(options));
    }
}

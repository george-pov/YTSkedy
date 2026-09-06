using Microsoft.Extensions.Logging;
using System.Net;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.TestSupport;
using static YTSkedy.Infrastructure.Test.WordPress.WordPressTestResponses;

namespace YTSkedy.Infrastructure.Test.WordPress;

public class WordPressEndpointResolverTests
{
    private const string ApplicationPassword = "application-password-secret";
    private readonly Mock<ILogger<WordPressEndpointResolver>> _logger = new();

    [Fact]
    public async Task ResolveAsync_LinkHeaderPrettyRoot_ReturnsRoot()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? LinkResponse("<https://example.com/wp-json/>; rel=\"https://api.w.org/\"")
                : JsonIndexResponse());
        var resolver = CreateResolver(handler);

        var root = await resolver.ResolveAsync(Settings(), CancellationToken.None);

        Assert.Equal("https://example.com/wp-json/", root.RootUri.ToString());
        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Head, request.Method);
                Assert.Equal("https://example.com/", request.RequestUri.ToString());
            },
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("https://example.com/wp-json/", request.RequestUri.ToString());
            });
    }

    [Fact]
    public async Task ResolveAsync_RepeatedSite_DiscoversOnce()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? LinkResponse("<https://example.com/wp-json/>; rel=\"https://api.w.org/\"")
                : JsonIndexResponse());
        var resolver = CreateResolver(handler);

        var first = await resolver.ResolveAsync(Settings(), CancellationToken.None);
        var second = await resolver.ResolveAsync(Settings(), CancellationToken.None);

        Assert.Equal(first, second);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task ResolveAsync_ExpiredCache_DiscoversAgain()
    {
        var now = new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero);
        var clock = new Mock<TimeProvider>();
        clock.Setup(provider => provider.GetUtcNow()).Returns(() => now);
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? LinkResponse("<https://example.com/wp-json/>; rel=\"https://api.w.org/\"")
                : JsonIndexResponse());
        var resolver = CreateResolver(handler, clock.Object);

        await resolver.ResolveAsync(Settings(), CancellationToken.None);
        now += TimeSpan.FromMinutes(5);
        await resolver.ResolveAsync(Settings(), CancellationToken.None);

        Assert.Equal(4, handler.CallCount);
    }

    [Fact]
    public async Task ResolveAsync_LinkHeaderRouteRoot_ReturnsRoot()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? LinkResponse("<https://example.com/index.php?rest_route=/>; rel=\"https://api.w.org/\"")
                : JsonIndexResponse());
        var resolver = CreateResolver(handler);

        var root = await resolver.ResolveAsync(Settings(), CancellationToken.None);

        Assert.Equal("https://example.com/index.php?rest_route=/", root.RootUri.ToString());
        Assert.Equal(
            "https://example.com/index.php?rest_route=/",
            handler.Requests.Single(request => request.Method == HttpMethod.Get).RequestUri.ToString());
    }

    [Fact]
    public async Task ResolveAsync_LinkHeaderDifferentHost_IgnoresLinkedRoot()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? LinkResponse("<https://other.example.com/wp-json/>; rel=\"https://api.w.org/\"")
                : JsonIndexResponse());
        var resolver = CreateResolver(handler);

        var root = await resolver.ResolveAsync(Settings(), CancellationToken.None);

        Assert.Equal("https://example.com/wp-json/", root.RootUri.ToString());
        Assert.DoesNotContain(
            handler.Requests,
            request => request.RequestUri.Host == "other.example.com");
    }

    [Fact]
    public async Task ResolveAsync_LinkHeaderDowngradesHttps_IgnoresLinkedRoot()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? LinkResponse("<http://example.com/wp-json/>; rel=\"https://api.w.org/\"")
                : JsonIndexResponse());
        var resolver = CreateResolver(handler);

        var root = await resolver.ResolveAsync(Settings(), CancellationToken.None);

        Assert.Equal("https://example.com/wp-json/", root.RootUri.ToString());
        Assert.DoesNotContain(
            handler.Requests,
            request => request.RequestUri.Scheme == Uri.UriSchemeHttp);
    }

    [Fact]
    public async Task ResolveAsync_UnsafeLinkedRootAndBrokenFallbacks_ThrowsWithoutUnsafeProbe()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? LinkResponse("<https://other.example.com/wp-json/>; rel=\"https://api.w.org/\"")
                : HtmlResponse());
        var resolver = CreateResolver(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => resolver.ResolveAsync(Settings(), CancellationToken.None));

        Assert.DoesNotContain(
            handler.Requests,
            request => request.RequestUri.Host == "other.example.com");
    }

    [Fact]
    public async Task ResolveAsync_MissingLinkHeader_ProbesPrettyRoot()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : JsonIndexResponse());
        var resolver = CreateResolver(handler);

        var root = await resolver.ResolveAsync(Settings(), CancellationToken.None);

        Assert.Equal("https://example.com/wp-json/", root.RootUri.ToString());
        Assert.Equal(
            "https://example.com/wp-json/",
            handler.Requests.Single(request => request.Method == HttpMethod.Get).RequestUri.ToString());
    }

    [Fact]
    public async Task ResolveAsync_BrokenPrettyRoot_ProbesRouteRoot()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Head)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return request.RequestUri!.ToString() == "https://example.com/wp-json/"
                ? HtmlResponse()
                : JsonIndexResponse();
        });
        var resolver = CreateResolver(handler);

        var root = await resolver.ResolveAsync(Settings(), CancellationToken.None);

        Assert.Equal("https://example.com/index.php?rest_route=/", root.RootUri.ToString());
        Assert.Equal(
            [
                "https://example.com/",
                "https://example.com/wp-json/",
                "https://example.com/index.php?rest_route=/"
            ],
            handler.Requests.Select(request => request.RequestUri.ToString()).ToArray());
    }

    [Fact]
    public async Task ResolveAsync_InvalidJson_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : JsonResponse("{not-json"));
        var resolver = CreateResolver(handler);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => resolver.ResolveAsync(Settings(), CancellationToken.None));

        Assert.Contains("example.com", exception.Message);
        Assert.DoesNotContain(ApplicationPassword, exception.Message);
        Assert.DoesNotContain(ApplicationPassword, _logger.GetLogText());
    }

    [Fact]
    public async Task ResolveAsync_UnsupportedApiIndex_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : JsonResponse("""{"namespaces":["oembed/1.0"],"routes":{"/":{}}}"""));
        var resolver = CreateResolver(handler);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => resolver.ResolveAsync(Settings(), CancellationToken.None));

        Assert.Contains("example.com", exception.Message);
        Assert.DoesNotContain(ApplicationPassword, exception.Message);
    }

    [Fact]
    public void BuildRoute_PrettyRoot_AppendsLogicalRoute()
    {
        var root = new WordPressRoot(new Uri("https://example.com/wp-json/"));

        var endpoint = root.BuildRoute("/wp/v2/posts");

        Assert.Equal("https://example.com/wp-json/wp/v2/posts", endpoint.ToString());
    }

    [Fact]
    public void BuildRoute_RouteRoot_UpdatesRouteQuery()
    {
        var root = new WordPressRoot(new Uri("https://example.com/index.php?rest_route=/"));

        var endpoint = root.BuildRoute("/wp/v2/posts");

        Assert.Equal(
            "https://example.com/index.php?rest_route=/wp/v2/posts",
            endpoint.ToString());
    }

    [Fact]
    public void BuildRoute_RouteRoot_AppendsAdditionalQuery()
    {
        var root = new WordPressRoot(new Uri("https://example.com/index.php?rest_route=/"));

        var endpoint = root.BuildRoute(
            "/wp/v2/posts/74",
            new Dictionary<string, string> { ["force"] = "true" });

        Assert.Equal(
            "https://example.com/index.php?rest_route=/wp/v2/posts/74&force=true",
            endpoint.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("wp/v2/posts")]
    public void BuildRoute_InvalidRoute_ThrowsArgumentException(string route)
    {
        var root = new WordPressRoot(new Uri("https://example.com/wp-json/"));

        Assert.Throws<ArgumentException>(() => root.BuildRoute(route));
    }

    [Fact]
    public async Task ResolveAsync_DiscoveryDoesNotSendAuthorizationHeader()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? LinkResponse("<https://example.com/wp-json/>; rel=\"https://api.w.org/\"")
                : JsonIndexResponse());
        var resolver = CreateResolver(handler);

        await resolver.ResolveAsync(Settings(), CancellationToken.None);

        Assert.All(handler.Requests, request => Assert.Null(request.Authorization));
    }

    [Fact]
    public async Task ResolveAsync_FailureLogsSecretSafeContext()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : JsonResponse("""{"namespaces":["oembed/1.0"]}"""));
        var resolver = CreateResolver(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => resolver.ResolveAsync(Settings(), CancellationToken.None));

        var logText = _logger.GetLogText();
        Assert.Contains("example.com", logText);
        Assert.DoesNotContain(ApplicationPassword, logText);
        Assert.DoesNotContain("Basic", logText);
    }

    [Fact]
    public async Task ResolveAsync_RoutesApiIndex_ReturnsRoot()
    {
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? LinkResponse("<https://example.com/wp-json/>; rel=\"https://api.w.org/\"")
                : JsonResponse("""{"routes":{"/wp/v2/posts":{}}}"""));
        var resolver = CreateResolver(handler);

        var root = await resolver.ResolveAsync(Settings(), CancellationToken.None);

        Assert.Equal("https://example.com/wp-json/", root.RootUri.ToString());
    }

    private WordPressEndpointResolver CreateResolver(
        FakeHttpMessageHandler handler,
        TimeProvider? timeProvider = null) =>
        new(new HttpClient(handler), _logger.Object, timeProvider);

    private static WordPressSettings Settings(string siteUrl = "https://example.com") =>
        new(siteUrl, "editor", ApplicationPassword, "publish", []);
}

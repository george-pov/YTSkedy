using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.WordPress;

public class WordPressEndpointResolverTests
{
    private const string ApplicationPassword = "application-password-secret";

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
        var logger = new CapturingLogger<WordPressEndpointResolver>();
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : JsonResponse("{not-json"));
        var resolver = CreateResolver(handler, logger);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => resolver.ResolveAsync(Settings(), CancellationToken.None));

        Assert.Contains("example.com", exception.Message);
        Assert.DoesNotContain(ApplicationPassword, exception.Message);
        Assert.DoesNotContain(ApplicationPassword, LogText(logger));
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
        var logger = new CapturingLogger<WordPressEndpointResolver>();
        var handler = new FakeHttpMessageHandler(request =>
            request.Method == HttpMethod.Head
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : JsonResponse("""{"namespaces":["oembed/1.0"]}"""));
        var resolver = CreateResolver(handler, logger);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => resolver.ResolveAsync(Settings(), CancellationToken.None));

        var logText = LogText(logger);
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

    private static WordPressEndpointResolver CreateResolver(
        FakeHttpMessageHandler handler,
        ILogger<WordPressEndpointResolver>? logger = null) =>
        new(new HttpClient(handler), logger ?? new CapturingLogger<WordPressEndpointResolver>());

    private static WordPressSettings Settings(string siteUrl = "https://example.com") =>
        new(siteUrl, "editor", ApplicationPassword, "publish");

    private static HttpResponseMessage LinkResponse(string linkHeader)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("Link", linkHeader);

        return response;
    }

    private static HttpResponseMessage JsonIndexResponse() =>
        JsonResponse("""{"namespaces":["wp/v2"]}""");

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage HtmlResponse() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent("<html></html>", Encoding.UTF8, "text/html")
        };

    private static string LogText<T>(CapturingLogger<T> logger) =>
        string.Join(Environment.NewLine, logger.Entries.Select(entry => entry.Message));

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public List<RequestSnapshot> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RequestSnapshot(
                request.Method,
                request.RequestUri!,
                request.Headers.Authorization));

            return Task.FromResult(handler(request));
        }
    }

    private sealed record RequestSnapshot(
        HttpMethod Method,
        Uri RequestUri,
        AuthenticationHeaderValue? Authorization);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}

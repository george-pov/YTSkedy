using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using static YTSkedy.Infrastructure.Test.WordPress.WordPressTestResponses;

namespace YTSkedy.Infrastructure.Test.WordPress;

public class WordPressPublisherTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private const string ApplicationPassword = "application-password-secret";

    [Fact]
    public void Type_IsWordPress()
    {
        var publisher = CreatePublisher(PrettyRootHandler(_ => JsonResponse("""{"id":74}""")));

        Assert.Equal(PlatformType.WordPress, publisher.Type);
    }

    [Fact]
    public async Task PublishAsync_DiscoveredPrettyRoot_PostsExpectedRequestAndReturnsId()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = PrettyRootHandler(
            postHandler: request =>
            {
                capturedRequest = request;
                capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonResponse("""{"id":74,"link":"https://example.com/blog/post"}""");
            },
            rootUrl: "https://example.com/blog/wp-json/");
        var publisher = CreatePublisher(handler);

        var result = await publisher.PublishAsync(
            Request(new WordPressSettings(
                "https://example.com/blog/",
                "editor",
                ApplicationPassword,
                "publish")),
            CancellationToken.None);

        Assert.Equal("74", result.ExternalResourceId);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal(
            "https://example.com/blog/wp-json/wp/v2/posts",
            capturedRequest.RequestUri!.ToString());

        Assert.Equal("Basic", capturedRequest.Headers.Authorization!.Scheme);
        var expectedAuth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"editor:{ApplicationPassword}"));
        Assert.Equal(expectedAuth, capturedRequest.Headers.Authorization.Parameter);

        using var document = JsonDocument.Parse(capturedBody!);
        var root = document.RootElement;
        Assert.Equal("English title", root.GetProperty("title").GetString());
        Assert.Equal("English description", root.GetProperty("content").GetString());
        Assert.Equal("publish", root.GetProperty("status").GetString());

        AssertDiscoveryRequestsAreAnonymous(handler);
    }

    [Fact]
    public async Task PublishAsync_DiscoveredRouteRoot_PostsExpectedRequestAndReturnsId()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = RouteRootHandler(request =>
        {
            capturedRequest = request;
            return JsonResponse("""{"id":74}""");
        });
        var publisher = CreatePublisher(handler);

        var result = await publisher.PublishAsync(Request(), CancellationToken.None);

        Assert.Equal("74", result.ExternalResourceId);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal(
            "https://example.com/index.php?rest_route=/wp/v2/posts",
            capturedRequest.RequestUri!.ToString());
        Assert.Equal("Basic", capturedRequest.Headers.Authorization!.Scheme);
        AssertDiscoveryRequestsAreAnonymous(handler);
    }

    [Fact]
    public async Task PublishAsync_NullDescription_SendsEmptyContent()
    {
        string? capturedBody = null;
        var handler = PrettyRootHandler(request =>
        {
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse("""{"id":75}""");
        });
        var publisher = CreatePublisher(handler);

        await publisher.PublishAsync(
            Request(
                new WordPressSettings(
                    "https://example.com",
                    "editor",
                    ApplicationPassword,
                    "draft"),
                description: null),
            CancellationToken.None);

        using var document = JsonDocument.Parse(capturedBody!);
        Assert.Equal("", document.RootElement.GetProperty("content").GetString());
        Assert.Equal("draft", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PublishAsync_DiscoveryFailure_ThrowsPlatformPublishException()
    {
        var logger = new CapturingLogger<WordPressPublisher>();
        var handler = UnsupportedDiscoveryHandler();
        var publisher = CreatePublisher(handler, logger);

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(Request(), CancellationToken.None));

        Assert.Contains("Failed to publish", exception.Message);
        Assert.DoesNotContain(ApplicationPassword, exception.Message);
        Assert.DoesNotContain(ApplicationPassword, LogText(logger));
        Assert.Equal(3, handler.CallCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task PublishAsync_NonSuccessStatus_ThrowsAndLogsSecretSafeContext(
        HttpStatusCode statusCode)
    {
        var logger = new CapturingLogger<WordPressPublisher>();
        var handler = PrettyRootHandler(_ => new HttpResponseMessage(statusCode));
        var publisher = CreatePublisher(handler, logger);

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(Request(), CancellationToken.None));

        Assert.Contains(((int)statusCode).ToString(), exception.Message);
        Assert.DoesNotContain(ApplicationPassword, exception.Message);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(CalendarEventId, entry.Message);
        Assert.Contains(PlatformId, entry.Message);
        Assert.Contains("example.com", entry.Message);
        Assert.Contains(((int)statusCode).ToString(), entry.Message);
        Assert.DoesNotContain(ApplicationPassword, entry.Message);
        Assert.DoesNotContain("Basic", entry.Message);
    }

    [Fact]
    public async Task PublishAsync_MalformedJson_ThrowsPlatformPublishException()
    {
        var logger = new CapturingLogger<WordPressPublisher>();
        var handler = PrettyRootHandler(_ => JsonResponse("{not-json"));
        var publisher = CreatePublisher(handler, logger);

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(Request(), CancellationToken.None));

        Assert.Contains("malformed JSON", exception.Message);
        Assert.DoesNotContain(ApplicationPassword, exception.Message);
        Assert.Contains("malformed JSON", Assert.Single(logger.Entries).Message);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"id":0}""")]
    [InlineData("""{"id":-1}""")]
    public async Task PublishAsync_MissingOrInvalidId_ThrowsPlatformPublishException(
        string responseJson)
    {
        var logger = new CapturingLogger<WordPressPublisher>();
        var handler = PrettyRootHandler(_ => JsonResponse(responseJson));
        var publisher = CreatePublisher(handler, logger);

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(Request(), CancellationToken.None));

        Assert.Contains("invalid post id", exception.Message);
        Assert.DoesNotContain(ApplicationPassword, exception.Message);
        Assert.Contains("invalid post id", Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task PublishAsync_HttpRequestException_ThrowsPlatformPublishException()
    {
        var logger = new CapturingLogger<WordPressPublisher>();
        var handler = PrettyRootHandler(_ => throw new HttpRequestException("network down"));
        var publisher = CreatePublisher(handler, logger);

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(Request(), CancellationToken.None));

        Assert.Contains("Failed to publish", exception.Message);
        Assert.DoesNotContain(ApplicationPassword, exception.Message);
        Assert.Contains("example.com", Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task PublishAsync_OperationCanceledException_Propagates()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new OperationCanceledException());
        var publisher = CreatePublisher(handler);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => publisher.PublishAsync(Request(), CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_NonWordPressSettings_ThrowsWithoutProviderCall()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException());
        var publisher = CreatePublisher(handler);

        await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(
                Request(new YouTubeSettings(
                    new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
                    "private",
                    false)),
                CancellationToken.None));
        Assert.Equal(0, handler.CallCount);
    }

    private static WordPressPublisher CreatePublisher(
        FakeHttpMessageHandler handler,
        ILogger<WordPressPublisher>? logger = null)
    {
        var resolver = new WordPressEndpointResolver(
            new HttpClient(handler),
            new CapturingLogger<WordPressEndpointResolver>());

        return new WordPressPublisher(
            new HttpClient(handler),
            resolver,
            logger ?? new CapturingLogger<WordPressPublisher>());
    }

    private static FakeHttpMessageHandler PrettyRootHandler(
        Func<HttpRequestMessage, HttpResponseMessage> postHandler,
        string rootUrl = "https://example.com/wp-json/") =>
        new(request =>
        {
            if (request.Method == HttpMethod.Head)
            {
                return LinkResponse($"<{rootUrl}>; rel=\"https://api.w.org/\"");
            }

            if (request.Method == HttpMethod.Get)
            {
                return JsonIndexResponse();
            }

            return postHandler(request);
        });

    private static FakeHttpMessageHandler RouteRootHandler(
        Func<HttpRequestMessage, HttpResponseMessage> postHandler) =>
        new(request =>
        {
            if (request.Method == HttpMethod.Head)
            {
                return LinkResponse("<https://example.com/index.php?rest_route=/>; rel=\"https://api.w.org/\"");
            }

            if (request.Method == HttpMethod.Get)
            {
                return JsonIndexResponse();
            }

            return postHandler(request);
        });

    private static FakeHttpMessageHandler UnsupportedDiscoveryHandler() =>
        new(request =>
            request.Method == HttpMethod.Head
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : JsonResponse("""{"namespaces":["oembed/1.0"]}"""));

    private static PlatformPublishRequest Request(
        PublishSettings? settings = null,
        string? description = "English description") =>
        new(
            CalendarEventId,
            PlatformId,
            settings ?? new WordPressSettings(
                "https://example.com",
                "editor",
                ApplicationPassword,
                "publish"),
            "English title",
            description,
            new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero));

    private static void AssertDiscoveryRequestsAreAnonymous(FakeHttpMessageHandler handler)
    {
        var discoveryRequests = handler.Requests.Where(request =>
            request.Method == HttpMethod.Head || request.Method == HttpMethod.Get);

        Assert.All(discoveryRequests, request => Assert.Null(request.Authorization));
    }

    private static string LogText(CapturingLogger<WordPressPublisher> logger) =>
        string.Join(Environment.NewLine, logger.Entries.Select(entry => entry.Message));
}

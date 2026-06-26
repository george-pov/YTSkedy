using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.WordPress;

public class WordPressPublisherTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private const string ApplicationPassword = "application-password-secret";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Type_IsWordPress()
    {
        var publisher = CreatePublisher(new FakeHttpMessageHandler(_ => JsonResponse("""{"id":74}""")));

        Assert.Equal(PlatformType.WordPress, publisher.Type);
    }

    [Fact]
    public async Task PublishAsync_Success_PostsExpectedRequestAndReturnsId()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse("""{"id":74}""");
        });
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
    }

    [Fact]
    public async Task PublishAsync_NullDescription_SendsEmptyContent()
    {
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(request =>
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

    [Theory]
    [InlineData("https://example.com", "https://example.com/wp-json/wp/v2/posts")]
    [InlineData("https://example.com/", "https://example.com/wp-json/wp/v2/posts")]
    [InlineData("https://example.com/blog", "https://example.com/blog/wp-json/wp/v2/posts")]
    [InlineData("http://localhost:8000/", "http://localhost:8000/wp-json/wp/v2/posts")]
    public async Task PublishAsync_SiteUrl_BuildsNormalizedEndpoint(
        string siteUrl,
        string expectedEndpoint)
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return JsonResponse("""{"id":74}""");
        });
        var publisher = CreatePublisher(handler);

        await publisher.PublishAsync(
            Request(new WordPressSettings(siteUrl, "editor", ApplicationPassword, "publish")),
            CancellationToken.None);

        Assert.Equal(expectedEndpoint, capturedRequest!.RequestUri!.ToString());
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
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(statusCode));
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
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{not-json"));
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
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(responseJson));
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
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("network down"));
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
    public async Task PublishAsync_NonWordPressSettings_Throws()
    {
        var publisher = CreatePublisher(new FakeHttpMessageHandler(_ => JsonResponse("""{"id":74}""")));

        await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(
                Request(new YouTubeSettings("creds", "private", false)),
                CancellationToken.None));
    }

    private static WordPressPublisher CreatePublisher(
        FakeHttpMessageHandler handler,
        ILogger<WordPressPublisher>? logger = null) =>
        new(new HttpClient(handler), logger ?? new CapturingLogger<WordPressPublisher>());

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

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

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }

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

using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.WordPress;

public class WordPressPublicationDeleterTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private const string ApplicationPassword = "application-password-secret";

    [Fact]
    public void Type_IsWordPress()
    {
        var deleter = CreateDeleter(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.Equal(PlatformType.WordPress, deleter.Type);
    }

    [Fact]
    public async Task DeleteAsync_Success_DeletesExpectedPostWithForceAndReturnsDeleted()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var deleter = CreateDeleter(handler);

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Deleted, result.Status);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Delete, capturedRequest.Method);
        Assert.Equal(
            "https://example.com/wp-json/wp/v2/posts/74?force=true",
            capturedRequest.RequestUri!.ToString());

        Assert.Equal("Basic", capturedRequest.Headers.Authorization!.Scheme);
        var expectedAuth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"editor:{ApplicationPassword}"));
        Assert.Equal(expectedAuth, capturedRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsAlreadyGone()
    {
        var deleter = CreateDeleter(
            new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.AlreadyGone, result.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task DeleteAsync_ProviderOrAuthorizationFailure_ReturnsFailed(
        HttpStatusCode statusCode)
    {
        var logger = new CapturingLogger<WordPressPublicationDeleter>();
        var deleter = CreateDeleter(
            new FakeHttpMessageHandler(_ => new HttpResponseMessage(statusCode)),
            logger);

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Failed, result.Status);
        var logText = LogText(logger);
        Assert.Contains(((int)statusCode).ToString(), logText);
        Assert.Contains("example.com", logText);
        Assert.DoesNotContain(ApplicationPassword, logText);
        Assert.DoesNotContain("Basic", logText);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task DeleteAsync_InvalidPostId_ReturnsStateConflictWithoutProviderCall(
        string externalResourceId)
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException());
        var deleter = CreateDeleter(handler);

        var result = await deleter.DeleteAsync(
            Request(externalResourceId: externalResourceId),
            CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.StateConflict, result.Status);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task DeleteAsync_NonWordPressSettings_ReturnsFailed()
    {
        var deleter = CreateDeleter(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await deleter.DeleteAsync(
            Request(
                new YouTubeSettings(
                    new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
                    "private",
                    false)),
            CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Failed, result.Status);
    }

    [Fact]
    public async Task DeleteAsync_HttpRequestException_ReturnsFailedAndLogsSecretSafeContext()
    {
        var logger = new CapturingLogger<WordPressPublicationDeleter>();
        var deleter = CreateDeleter(
            new FakeHttpMessageHandler(_ => throw new HttpRequestException("network down")),
            logger);

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Failed, result.Status);
        var logText = LogText(logger);
        Assert.Contains("example.com", logText);
        Assert.DoesNotContain(ApplicationPassword, logText);
        Assert.DoesNotContain("Basic", logText);
    }

    [Fact]
    public async Task DeleteAsync_OperationCanceledException_Propagates()
    {
        var deleter = CreateDeleter(
            new FakeHttpMessageHandler(_ => throw new OperationCanceledException()));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => deleter.DeleteAsync(Request(), CancellationToken.None));
    }

    private static WordPressPublicationDeleter CreateDeleter(
        FakeHttpMessageHandler handler,
        ILogger<WordPressPublicationDeleter>? logger = null) =>
        new(new HttpClient(handler), logger ?? new CapturingLogger<WordPressPublicationDeleter>());

    private static PublicationDeleteRequest Request(
        PublishSettings? settings = null,
        string externalResourceId = "74") =>
        new(
            CalendarEventId,
            PlatformId,
            settings ?? new WordPressSettings(
                "https://example.com",
                "editor",
                ApplicationPassword,
                "publish"),
            externalResourceId);

    private static string LogText(CapturingLogger<WordPressPublicationDeleter> logger) =>
        string.Join(Environment.NewLine, logger.Entries.Select(entry => entry.Message));

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;

            return Task.FromResult(handler(request));
        }
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

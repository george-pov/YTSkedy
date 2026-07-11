using Microsoft.Extensions.Logging;
using static YTSkedy.Infrastructure.Test.WordPress.WordPressDiscoveryHandlers;
using static YTSkedy.Infrastructure.Test.WordPress.WordPressTestResponses;
using System.Net;
using System.Text;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;

namespace YTSkedy.Infrastructure.Test.WordPress;

public class WordPressPublicationDeleterTests
{
    private const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    private const string PlatformId = SchedulingSampleIds.PlatformId;
    private const string ApplicationPassword = "application-password-secret";

    [Fact]
    public void Type_IsWordPress()
    {
        var deleter = CreateDeleter(PrettyRoot(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.Equal(PlatformType.WordPress, deleter.Type);
    }

    [Fact]
    public async Task DeleteAsync_DiscoveredPrettyRoot_DeletesExpectedPostWithForce()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = PrettyRoot(request =>
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
        AssertDiscoveryRequestsAreAnonymous(handler);
    }

    [Fact]
    public async Task DeleteAsync_DiscoveredRouteRoot_DeletesExpectedPostWithForce()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = RouteRoot(request =>
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
            "https://example.com/index.php?rest_route=/wp/v2/posts/74&force=true",
            capturedRequest.RequestUri!.ToString());
        Assert.Equal("Basic", capturedRequest.Headers.Authorization!.Scheme);
        AssertDiscoveryRequestsAreAnonymous(handler);
    }

    [Fact]
    public async Task DeleteAsync_DiscoveryFailure_ReturnsFailed()
    {
        var logger = new CapturingLogger<WordPressPublicationDeleter>();
        var handler = UnsupportedDiscovery();
        var deleter = CreateDeleter(handler, logger);

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Failed, result.Status);
        var logText = logger.Text;
        Assert.Contains("example.com", logText);
        Assert.DoesNotContain(ApplicationPassword, logText);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsAlreadyGone()
    {
        var deleter = CreateDeleter(
            PrettyRoot(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

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
            PrettyRoot(_ => new HttpResponseMessage(statusCode)),
            logger);

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Failed, result.Status);
        var logText = logger.Text;
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
    public async Task DeleteAsync_NonWordPressSettings_ReturnsFailedWithoutProviderCall()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException());
        var deleter = CreateDeleter(handler);

        var result = await deleter.DeleteAsync(
            Request(
                new YouTubeSettings(
                    new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
                    "private",
                    false)),
            CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Failed, result.Status);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task DeleteAsync_HttpRequestException_ReturnsFailedAndLogsSecretSafeContext()
    {
        var logger = new CapturingLogger<WordPressPublicationDeleter>();
        var deleter = CreateDeleter(
            PrettyRoot(_ => throw new HttpRequestException("network down")),
            logger);

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Failed, result.Status);
        var logText = logger.Text;
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
        ILogger<WordPressPublicationDeleter>? logger = null)
    {
        var resolver = new WordPressEndpointResolver(
            new HttpClient(handler),
            new CapturingLogger<WordPressEndpointResolver>());

        return new WordPressPublicationDeleter(
            new HttpClient(handler),
            resolver,
            logger ?? new CapturingLogger<WordPressPublicationDeleter>());
    }

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
                "publish",
                []),
            externalResourceId);
}

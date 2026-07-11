using Microsoft.Extensions.Logging;
using static YTSkedy.Infrastructure.Test.WordPress.WordPressDiscoveryHandlers;
using static YTSkedy.Infrastructure.Test.WordPress.WordPressTestResponses;
using System.Net;
using System.Text;
using System.Text.Json;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Infrastructure.WordPress;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.Infrastructure.Test.WordPress;

public class WordPressPublisherTests
{
    private const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    private const string PlatformId = SchedulingSampleIds.PlatformId;
    private const string ApplicationPassword = "application-password-secret";

    [Fact]
    public void Type_IsWordPress()
    {
        var publisher = CreatePublisher(PrettyRoot(_ => JsonResponse("""{"id":74}""")));

        Assert.Equal(PlatformType.WordPress, publisher.Type);
    }

    [Fact]
    public async Task PublishAsync_DiscoveredPrettyRoot_PostsExpectedRequestAndReturnsId()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = PrettyRoot(
            requestHandler: request =>
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
                "publish",
                [])),
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
        Assert.False(root.GetProperty("sticky").GetBoolean());
        Assert.False(root.TryGetProperty("date_gmt", out _));
        Assert.False(root.TryGetProperty("categories", out _));

        AssertDiscoveryRequestsAreAnonymous(handler);
    }

    [Fact]
    public async Task PublishAsync_SelectedCategoryIds_SerializesInSubmittedOrder()
    {
        var settings = new WordPressSettings(
            "https://example.com",
            "editor",
            ApplicationPassword,
            "draft",
            [12, 34]);

        var publishedPost = await PublishAndReadPostJsonAsync(settings);

        Assert.Equal(
            [12, 34],
            publishedPost.Body.GetProperty("categories")
                .EnumerateArray()
                .Select(item => item.GetInt64()));
    }

    [Fact]
    public async Task PublishAsync_EmptyCategoryIds_OmitsCategories()
    {
        var publishedPost = await PublishAndReadPostJsonAsync(
            new WordPressSettings(
                "https://example.com",
                "editor",
                ApplicationPassword,
                "draft",
                []));

        Assert.False(publishedPost.Body.TryGetProperty("categories", out _));
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("pending")]
    [InlineData("private")]
    [InlineData("future")]
    [InlineData("publish")]
    public async Task PublishAsync_AllowedPostStatus_SendsConfiguredStatus(string postStatus)
    {
        var settings = new WordPressSettings(
            "https://example.com",
            "editor",
            ApplicationPassword,
            postStatus,
            [],
            scheduleOffsetHours: postStatus == WordPressSettings.ScheduledPostStatus ? 25 : null);

        var publishedPost = await PublishAndReadPostJsonAsync(settings);

        Assert.Equal(postStatus, publishedPost.Body.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishAsync_StickySetting_SerializesSticky(bool sticky)
    {
        var settings = new WordPressSettings(
            "https://example.com",
            "editor",
            ApplicationPassword,
            "publish",
            [],
            sticky);

        var publishedPost = await PublishAndReadPostJsonAsync(settings);

        Assert.Equal(sticky, publishedPost.Body.GetProperty("sticky").GetBoolean());
    }

    [Fact]
    public async Task PublishAsync_FutureStatusWithScheduleOffset_SendsDateGmt()
    {
        var settings = new WordPressSettings(
            "https://example.com",
            "editor",
            ApplicationPassword,
            WordPressSettings.ScheduledPostStatus,
            [12, 34],
            scheduleOffsetHours: WordPressSettings.MaxScheduleOffsetHours);

        var publishedPost = await PublishAndReadPostJsonAsync(
            settings,
            scheduledStartUtc: new DateTimeOffset(2026, 7, 1, 17, 0, 0, TimeSpan.Zero));

        Assert.Equal(
            "2026-06-24T17:00:00Z",
            publishedPost.Body.GetProperty("date_gmt").GetString());
        Assert.Equal(
            [12, 34],
            publishedPost.Body.GetProperty("categories")
                .EnumerateArray()
                .Select(item => item.GetInt64()));
    }

    [Fact]
    public async Task PublishAsync_StaleScheduledPostTime_ThrowsBeforeProviderCall()
    {
        var logger = new CapturingLogger<WordPressPublisher>();
        var handler = PrettyRoot(_ => JsonResponse("""{"id":74}"""));
        var publisher = CreatePublisher(handler, logger);
        var settings = new WordPressSettings(
            "https://example.com",
            "editor",
            ApplicationPassword,
            WordPressSettings.ScheduledPostStatus,
            [],
            scheduleOffsetHours: 80);

        var exception = await Assert.ThrowsAsync<PlatformPublishValidationException>(
            () => publisher.PublishAsync(Request(settings), CancellationToken.None));

        Assert.Contains("scheduled post time", exception.Message);
        Assert.DoesNotContain(ApplicationPassword, exception.Message);
        Assert.DoesNotContain("Basic", exception.Message);
        Assert.DoesNotContain(ApplicationPassword, logger.Text);
        Assert.DoesNotContain("Basic", logger.Text);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task PublishAsync_DiscoveredRouteRoot_PostsExpectedRequestAndReturnsId()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = RouteRoot(request =>
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
        var publishedPost = await PublishAndReadPostJsonAsync(
            new WordPressSettings(
                "https://example.com",
                "editor",
                ApplicationPassword,
                "draft",
                []),
            description: null);

        Assert.Equal("", publishedPost.Body.GetProperty("content").GetString());
        Assert.Equal("draft", publishedPost.Body.GetProperty("status").GetString());
        Assert.False(publishedPost.Body.TryGetProperty("date_gmt", out _));
    }

    [Fact]
    public async Task PublishAsync_DiscoveryFailure_ThrowsPlatformPublishException()
    {
        var logger = new CapturingLogger<WordPressPublisher>();
        var handler = UnsupportedDiscovery();
        var publisher = CreatePublisher(handler, logger);

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(Request(), CancellationToken.None));

        Assert.Contains("Failed to publish", exception.Message);
        Assert.DoesNotContain(ApplicationPassword, exception.Message);
        Assert.DoesNotContain(ApplicationPassword, logger.Text);
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
        var handler = PrettyRoot(_ => new HttpResponseMessage(statusCode));
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
        var handler = PrettyRoot(_ => JsonResponse("{not-json"));
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
        var handler = PrettyRoot(_ => JsonResponse(responseJson));
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
        var handler = PrettyRoot(_ => throw new HttpRequestException("network down"));
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
            new FixedTimeProvider(SchedulingSampleTimes.Now),
            logger ?? new CapturingLogger<WordPressPublisher>());
    }

    private static async Task<PublishedPostJson> PublishAndReadPostJsonAsync(
        WordPressSettings settings,
        string? description = "English description",
        DateTimeOffset? scheduledStartUtc = null)
    {
        string? capturedBody = null;
        var handler = PrettyRoot(request =>
        {
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse("""{"id":74}""");
        });
        var publisher = CreatePublisher(handler);

        var result = await publisher.PublishAsync(
            Request(settings, description, scheduledStartUtc),
            CancellationToken.None);

        using var document = JsonDocument.Parse(capturedBody!);
        return new PublishedPostJson(
            result,
            document.RootElement.Clone(),
            handler);
    }

    private static PlatformPublishRequest Request(
        PublishSettings? settings = null,
        string? description = "English description",
        DateTimeOffset? scheduledStartUtc = null) =>
        new(
            CalendarEventId,
            PlatformId,
            settings ?? new WordPressSettings(
                "https://example.com",
                "editor",
                ApplicationPassword,
                "publish",
                []),
            "English title",
            description,
            scheduledStartUtc ?? new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero));

    private sealed record PublishedPostJson(
        PlatformPublishResult Result,
        JsonElement Body,
        FakeHttpMessageHandler Handler);
}

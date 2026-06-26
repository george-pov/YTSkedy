using Microsoft.Extensions.Logging;
using System.Net;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubePublicationDeleterTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private const string BroadcastId = "yt-broadcast-id";
    private const string ClientSecret = "client-secret-value";
    private const string RefreshToken = "refresh-token-value";

    [Fact]
    public void Type_IsYouTube()
    {
        var deleter = CreateDeleter(new FakeDeletionClient());

        Assert.Equal(PlatformType.YouTube, deleter.Type);
    }

    [Fact]
    public async Task DeleteAsync_Success_DeletesBroadcastAndReturnsDeleted()
    {
        var client = new FakeDeletionClient();
        var deleter = CreateDeleter(client);

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Deleted, result.Status);
        Assert.Equal(BroadcastId, client.BroadcastId);
        Assert.Equal("client-id", client.Credentials!.ClientId);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsAlreadyGone()
    {
        var deleter = CreateDeleter(
            new FakeDeletionClient
            {
                Throws = new YouTubePublicationDeleteException(HttpStatusCode.NotFound, [])
            });

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.AlreadyGone, result.Status);
    }

    [Fact]
    public async Task DeleteAsync_DeletionNotAllowed_ReturnsStateConflict()
    {
        var deleter = CreateDeleter(
            new FakeDeletionClient
            {
                Throws = new YouTubePublicationDeleteException(
                    HttpStatusCode.Forbidden,
                    ["liveBroadcastDeletionNotAllowed"])
            });

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.StateConflict, result.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task DeleteAsync_ProviderOrAuthorizationFailure_ReturnsFailed(
        HttpStatusCode statusCode)
    {
        var logger = new CapturingLogger<YouTubePublicationDeleter>();
        var deleter = CreateDeleter(
            new FakeDeletionClient
            {
                Throws = new YouTubePublicationDeleteException(statusCode, ["authError"])
            },
            logger);

        var result = await deleter.DeleteAsync(Request(), CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Failed, result.Status);
        Assert.DoesNotContain(ClientSecret, LogText(logger));
        Assert.DoesNotContain(RefreshToken, LogText(logger));
    }

    [Fact]
    public async Task DeleteAsync_NonYouTubeSettings_ReturnsFailed()
    {
        var deleter = CreateDeleter(new FakeDeletionClient());

        var result = await deleter.DeleteAsync(
            Request(new WordPressSettings(
                "https://example.com",
                "editor",
                "application-password",
                "publish")),
            CancellationToken.None);

        Assert.Equal(PublicationDeleteStatus.Failed, result.Status);
    }

    [Fact]
    public async Task DeleteAsync_OperationCanceledException_Propagates()
    {
        var deleter = CreateDeleter(
            new FakeDeletionClient
            {
                Throws = new OperationCanceledException()
            });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => deleter.DeleteAsync(Request(), CancellationToken.None));
    }

    private static YouTubePublicationDeleter CreateDeleter(
        FakeDeletionClient client,
        ILogger<YouTubePublicationDeleter>? logger = null) =>
        new(client, logger ?? new CapturingLogger<YouTubePublicationDeleter>());

    private static PublicationDeleteRequest Request(PublishSettings? settings = null) =>
        new(
            CalendarEventId,
            PlatformId,
            settings ?? new YouTubeSettings(
                new YouTubeCredentials("client-id", ClientSecret, RefreshToken),
                "private",
                false),
            BroadcastId);

    private static string LogText(CapturingLogger<YouTubePublicationDeleter> logger) =>
        string.Join(Environment.NewLine, logger.Entries.Select(entry => entry.Message));

    private sealed class FakeDeletionClient : IYouTubeLiveBroadcastDeletionClient
    {
        public YouTubeCredentials? Credentials { get; private set; }

        public string? BroadcastId { get; private set; }

        public Exception? Throws { get; init; }

        public Task DeleteAsync(
            YouTubeCredentials credentials,
            string broadcastId,
            CancellationToken cancellationToken)
        {
            Credentials = credentials;
            BroadcastId = broadcastId;

            if (Throws is not null)
            {
                throw Throws;
            }

            return Task.CompletedTask;
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

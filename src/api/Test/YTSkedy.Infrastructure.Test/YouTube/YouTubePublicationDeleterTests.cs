using Microsoft.Extensions.Logging;
using System.Net;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubePublicationDeleterTests
{
    private const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    private const string PlatformId = SchedulingSampleIds.PlatformId;
    private const string BroadcastId = SchedulingSampleIds.YouTubeBroadcastId;
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
        Assert.DoesNotContain(ClientSecret, logger.Text);
        Assert.DoesNotContain(RefreshToken, logger.Text);
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

}

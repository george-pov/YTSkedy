using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.YouTube;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubePublisherTests
{
    private static readonly DateTimeOffset ScheduledStartUtc =
        new(2026, 06, 15, 17, 00, 00, TimeSpan.Zero);

    private static readonly YouTubeRequest Request =
        new("English title", "English description", ScheduledStartUtc);

    [Fact]
    public async Task PublishAsync_Success_ReturnsBroadcastIdAndPassesRequestToClient()
    {
        var client = new FakeYouTubeClient { BroadcastId = "broadcast-123" };
        var publisher = CreatePublisher(client);

        var broadcastId = await publisher.PublishAsync(Request, CancellationToken.None);

        Assert.Equal("broadcast-123", broadcastId);
        Assert.Equal(1, client.InsertCallCount);
        Assert.Same(Request, client.InsertedRequest);
    }

    [Fact]
    public async Task PublishAsync_ProviderFailure_RethrowsSameException()
    {
        var providerFailure = new InvalidOperationException("YouTube insert failed");
        var client = new FakeYouTubeClient { Failure = providerFailure };
        var publisher = CreatePublisher(client);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(Request, CancellationToken.None));

        Assert.Same(providerFailure, thrown);
    }

    [Fact]
    public async Task PublishAsync_NullRequest_ThrowsWithoutCallingClient()
    {
        var client = new FakeYouTubeClient { BroadcastId = "broadcast-123" };
        var publisher = CreatePublisher(client);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher.PublishAsync(null!, CancellationToken.None));

        Assert.Equal(0, client.InsertCallCount);
    }

    private static YouTubePublisher CreatePublisher(IYouTubeClient client) =>
        new(client, NullLogger<YouTubePublisher>.Instance);

    private sealed class FakeYouTubeClient : IYouTubeClient
    {
        public string BroadcastId { get; init; } = "broadcast-123";

        public Exception? Failure { get; init; }

        public int InsertCallCount { get; private set; }

        public YouTubeRequest? InsertedRequest { get; private set; }

        public Task<string> InsertAsync(
            YouTubeRequest request,
            CancellationToken cancellationToken)
        {
            InsertCallCount++;
            InsertedRequest = request;

            return Failure is null
                ? Task.FromResult(BroadcastId)
                : Task.FromException<string>(Failure);
        }

        public Task<YouTubeDeleteResult> DeleteAsync(
            string broadcastId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.YouTube;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubeDeleterTests
{
    private const string BroadcastId = "broadcast-123";

    [Theory]
    [InlineData(YouTubeDeleteResult.Deleted)]
    [InlineData(YouTubeDeleteResult.NotFound)]
    public async Task DeleteAsync_ProviderDeletedOrAlreadyGone_CompletesAndCallsProviderOnce(
        YouTubeDeleteResult providerResult)
    {
        // A fresh delete and an already-gone broadcast are both success-equivalent:
        // the adapter completes without throwing and calls the provider once.
        var client = new FakeYouTubeClient { Result = providerResult };
        var deleter = CreateDeleter(client);

        await deleter.DeleteAsync(BroadcastId, CancellationToken.None);

        Assert.Equal(1, client.CallCount);
        Assert.Equal(BroadcastId, client.RequestedBroadcastId);
    }

    [Fact]
    public async Task DeleteAsync_ProviderFailure_ThrowsYouTubeDeleteExceptionPreservingCause()
    {
        var providerFailure = new InvalidOperationException("quota exceeded");
        var client = new FakeYouTubeClient { Failure = providerFailure };
        var deleter = CreateDeleter(client);

        var exception = await Assert.ThrowsAsync<YouTubeDeleteException>(
            () => deleter.DeleteAsync(BroadcastId, CancellationToken.None));

        Assert.Same(providerFailure, exception.InnerException);
    }

    [Fact]
    public async Task DeleteAsync_Cancellation_PropagatesWithoutWrapping()
    {
        var client = new FakeYouTubeClient
        {
            Failure = new OperationCanceledException()
        };
        var deleter = CreateDeleter(client);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => deleter.DeleteAsync(BroadcastId, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteAsync_BlankBroadcastId_ThrowsWithoutCallingProvider(string broadcastId)
    {
        var client = new FakeYouTubeClient();
        var deleter = CreateDeleter(client);

        await Assert.ThrowsAsync<ArgumentException>(
            () => deleter.DeleteAsync(broadcastId, CancellationToken.None));

        Assert.Equal(0, client.CallCount);
    }

    private static YouTubeDeleter CreateDeleter(IYouTubeClient client) =>
        new(client, NullLogger<YouTubeDeleter>.Instance);

    private sealed class FakeYouTubeClient : IYouTubeClient
    {
        public YouTubeDeleteResult Result { get; init; } =
            YouTubeDeleteResult.Deleted;

        public Exception? Failure { get; init; }

        public int CallCount { get; private set; }

        public string? RequestedBroadcastId { get; private set; }

        public Task<string> InsertAsync(
            YouTubeRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<YouTubeDeleteResult> DeleteAsync(
            string broadcastId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            RequestedBroadcastId = broadcastId;

            return Failure is null
                ? Task.FromResult(Result)
                : Task.FromException<YouTubeDeleteResult>(Failure);
        }
    }
}

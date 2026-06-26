using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class DeletePlatformHandlerTests
{
    private const string PlatformId = "p1";

    private static readonly PlatformView ExistingPlatform = new(
        PlatformId,
        "Main channel",
        PlatformType.YouTube,
        new YouTubeSettings(
            new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
            "private",
            false));

    [Fact]
    public async Task HandleAsync_NoPublishingRows_OrphansThenDeletesAndReturnsDeleted()
    {
        var calls = new List<string>();
        var reader = new FakePlatformReader(ExistingPlatform);
        var modifier = new FakePlatformModifier(calls) { DeleteResult = DeletePlatformResult.Deleted };
        var publicationReader = new FakePlatformPublicationReader();
        var publicationRepository = new FakePlatformPublicationRepository(calls);
        var handler = new DeletePlatformHandler(
            reader,
            modifier,
            publicationReader,
            publicationRepository);

        var result = await handler.HandleAsync(
            new DeletePlatformCommand(PlatformId),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.Deleted, result);
        Assert.Equal(PlatformId, publicationReader.PublishingPlatformId);
        Assert.Equal(PlatformId, publicationRepository.OrphanedPlatformId);
        Assert.Equal(PlatformId, modifier.DeletedPlatformId);

        // History must be orphaned before the platform row is removed so a crash
        // after the delete cannot leave published rows without a deleted marker.
        Assert.Equal(["orphan", "delete"], calls);
    }

    [Fact]
    public async Task HandleAsync_PlatformMissing_ReturnsNotFoundWithoutOrphanOrDelete()
    {
        var calls = new List<string>();
        var reader = new FakePlatformReader(null);
        var modifier = new FakePlatformModifier(calls);
        var publicationReader = new FakePlatformPublicationReader();
        var publicationRepository = new FakePlatformPublicationRepository(calls);
        var handler = new DeletePlatformHandler(
            reader,
            modifier,
            publicationReader,
            publicationRepository);

        var result = await handler.HandleAsync(
            new DeletePlatformCommand("missing"),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.NotFound, result);
        Assert.Null(publicationRepository.OrphanedPlatformId);
        Assert.Null(modifier.DeletedPlatformId);
        Assert.Empty(calls);
    }

    [Fact]
    public async Task HandleAsync_PublishingRowExists_ReturnsConflictWithoutOrphanOrDelete()
    {
        var calls = new List<string>();
        var reader = new FakePlatformReader(ExistingPlatform);
        var modifier = new FakePlatformModifier(calls);
        var publicationReader = new FakePlatformPublicationReader
        {
            Publishing = [CreatePublication(PublishStatus.Publishing)]
        };
        var publicationRepository = new FakePlatformPublicationRepository(calls);
        var handler = new DeletePlatformHandler(
            reader,
            modifier,
            publicationReader,
            publicationRepository);

        var result = await handler.HandleAsync(
            new DeletePlatformCommand(PlatformId),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.Conflict, result);
        Assert.Null(publicationRepository.OrphanedPlatformId);
        Assert.Null(modifier.DeletedPlatformId);
        Assert.Empty(calls);
    }

    [Fact]
    public async Task HandleAsync_DeleteRacesToNotFound_ReturnsNotFoundAfterOrphaning()
    {
        var calls = new List<string>();
        var reader = new FakePlatformReader(ExistingPlatform);
        var modifier = new FakePlatformModifier(calls) { DeleteResult = DeletePlatformResult.NotFound };
        var publicationReader = new FakePlatformPublicationReader();
        var publicationRepository = new FakePlatformPublicationRepository(calls);
        var handler = new DeletePlatformHandler(
            reader,
            modifier,
            publicationReader,
            publicationRepository);

        var result = await handler.HandleAsync(
            new DeletePlatformCommand(PlatformId),
            CancellationToken.None);

        Assert.Equal(DeletePlatformResult.NotFound, result);
        Assert.Equal(PlatformId, publicationRepository.OrphanedPlatformId);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var calls = new List<string>();
        var handler = new DeletePlatformHandler(
            new FakePlatformReader(ExistingPlatform),
            new FakePlatformModifier(calls),
            new FakePlatformPublicationReader(),
            new FakePlatformPublicationRepository(calls));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private static PlatformPublication CreatePublication(PublishStatus status) =>
        new(
            "20260615T170000Z",
            PlatformId,
            "Main channel",
            PlatformType.YouTube,
            status,
            null,
            null,
            null,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

    private sealed class FakePlatformReader(PlatformView? platform) : IPlatformReader
    {
        public Task<IReadOnlyList<PlatformView>> ListAsync(
            PlatformType? type,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PlatformView?> GetAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            Task.FromResult(platform);
    }

    private sealed class FakePlatformModifier(List<string> calls) : IPlatformModifier
    {
        public DeletePlatformResult DeleteResult { get; init; } = DeletePlatformResult.Deleted;

        public string? DeletedPlatformId { get; private set; }

        public Task<CreatePlatformResult> CreateAsync(
            Platform platform,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UpdatePlatformResult> UpdateAsync(
            string platformId,
            string name,
            PublishSettings publishSettings,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeletePlatformResult> DeleteAsync(
            string platformId,
            CancellationToken cancellationToken)
        {
            calls.Add("delete");
            DeletedPlatformId = platformId;

            return Task.FromResult(DeleteResult);
        }
    }

    private sealed class FakePlatformPublicationReader : IPlatformPublicationReader
    {
        public IReadOnlyList<PlatformPublication> Publishing { get; init; } = [];

        public string? PublishingPlatformId { get; private set; }

        public Task<IReadOnlyList<PlatformPublication>> ListByEventAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PlatformPublication?> GetAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlatformPublication>> ListPublishingByPlatformAsync(
            string platformId,
            CancellationToken cancellationToken)
        {
            PublishingPlatformId = platformId;

            return Task.FromResult(Publishing);
        }
    }

    private sealed class FakePlatformPublicationRepository(List<string> calls)
        : IPlatformPublicationRepository
    {
        public string? OrphanedPlatformId { get; private set; }

        public Task<StartPublicationResult> StartPublishingAsync(
            PlatformPublicationAttempt attempt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ReleasePublishingAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DateTimeOffset?> MarkPublishedAsync(
            string calendarEventId,
            string platformId,
            string externalResourceId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> OrphanPublishedByPlatformAsync(
            string platformId,
            CancellationToken cancellationToken)
        {
            calls.Add("orphan");
            OrphanedPlatformId = platformId;

            return Task.FromResult(0);
        }
    }
}

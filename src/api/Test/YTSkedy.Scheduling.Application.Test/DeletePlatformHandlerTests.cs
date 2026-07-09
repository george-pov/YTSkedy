using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class DeletePlatformHandlerTests
{
    private const string PlatformId = "p1";

    private static readonly PlatformView ExistingPlatform = ApplicationTestData.Platform(
        platformId: PlatformId,
        name: "Main channel");

    [Fact]
    public async Task HandleAsync_NoPublishingRows_OrphansThenDeletesAndReturnsDeleted()
    {
        var calls = new List<string>();
        var reader = new FakePlatformReader(getResult: ExistingPlatform);
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
        var reader = new FakePlatformReader();
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
        var reader = new FakePlatformReader(getResult: ExistingPlatform);
        var modifier = new FakePlatformModifier(calls);
        var publicationReader = new FakePlatformPublicationReader(
            [CreatePublication(PublishStatus.Publishing)]);
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
        var reader = new FakePlatformReader(getResult: ExistingPlatform);
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
            new FakePlatformReader(getResult: ExistingPlatform),
            new FakePlatformModifier(calls),
            new FakePlatformPublicationReader(),
            new FakePlatformPublicationRepository(calls));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private static PlatformPublication CreatePublication(PublishStatus status) =>
        ApplicationTestData.Publication(
            status,
            platformId: PlatformId,
            platformName: "Main channel");

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
            string? referenceKey,
            PublishSettings publishSettings,
            PublishingContent publishingContent,
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

        public Task<bool> MarkThumbnailAppliedAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> MarkThumbnailFailedAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeletePublishedResult> DeletePublishedAsync(
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

using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.Infrastructure.Test.Platforms;

public class AzurePlatformPublicationRepositoryTests
{
    private const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    private const string OtherCalendarEventId = SchedulingSampleIds.OtherCalendarEventId;
    private const string PlatformId = SchedulingSampleIds.PlatformId;

    [Fact]
    public async Task StartAndMarkPublished_PreservesContentSnapshot()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        var attempt = new PlatformPublicationAttempt(
            CalendarEventId,
            PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            SchedulingSamples.YouTubeSettings(),
            new ContentSnapshot("Rendered title", "Rendered description"));

        var start = await repository.StartPublishingAsync(attempt, CancellationToken.None);
        var publishing = await repository.GetAsync(CalendarEventId, PlatformId, CancellationToken.None);
        var publishedUtc = await repository.MarkPublishedAsync(
            CalendarEventId,
            PlatformId,
            SchedulingSampleIds.YouTubeBroadcastId,
            CancellationToken.None);
        var published = await repository.GetAsync(CalendarEventId, PlatformId, CancellationToken.None);

        Assert.Equal(StartPublicationResult.Started, start);
        Assert.NotNull(publishing);
        Assert.Equal(PublishStatus.Publishing, publishing.Status);
        Assert.Equal("Rendered title", publishing.ContentSnapshot!.Title);
        Assert.Equal("Rendered description", publishing.ContentSnapshot.Description);

        Assert.NotNull(publishedUtc);
        Assert.NotNull(published);
        Assert.Equal(PublishStatus.Published, published.Status);
        Assert.Equal(SchedulingSampleIds.YouTubeBroadcastId, published.ExternalResourceId);
        Assert.Equal(ThumbnailPublishStatus.NotConfigured, published.ThumbnailStatus);
        Assert.Equal("Rendered title", published.ContentSnapshot!.Title);
        Assert.Equal("Rendered description", published.ContentSnapshot.Description);
    }

    [Fact]
    public async Task MarkThumbnailApplied_PublishedRow_StoresApplied()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        await StartAndPublish(repository);

        var updated = await repository.MarkThumbnailAppliedAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None);
        var publication = await repository.GetAsync(CalendarEventId, PlatformId, CancellationToken.None);

        Assert.True(updated);
        Assert.Equal(ThumbnailPublishStatus.Applied, publication!.ThumbnailStatus);
    }

    [Fact]
    public async Task MarkThumbnailFailed_PublishedRow_StoresFailed()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        await StartAndPublish(repository);

        var updated = await repository.MarkThumbnailFailedAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None);
        var publication = await repository.GetAsync(CalendarEventId, PlatformId, CancellationToken.None);

        Assert.True(updated);
        Assert.Equal(ThumbnailPublishStatus.Failed, publication!.ThumbnailStatus);
    }

    [Fact]
    public async Task MarkThumbnailApplied_PublishingRow_ReturnsFalse()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        await repository.StartPublishingAsync(Attempt(), CancellationToken.None);

        var updated = await repository.MarkThumbnailAppliedAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None);
        var publication = await repository.GetAsync(CalendarEventId, PlatformId, CancellationToken.None);

        Assert.False(updated);
        Assert.Equal(ThumbnailPublishStatus.NotConfigured, publication!.ThumbnailStatus);
    }

    [Fact]
    public async Task HasAnyForEventAsync_NoRowsForEvent_ReturnsFalse()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        await repository.StartPublishingAsync(Attempt(OtherCalendarEventId), CancellationToken.None);

        var hasAny = await repository.HasAnyForEventAsync(
            CalendarEventId,
            CancellationToken.None);

        Assert.False(hasAny);
    }

    [Fact]
    public async Task HasAnyForEventAsync_RowExistsForEvent_ReturnsTrue()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        await repository.StartPublishingAsync(Attempt(CalendarEventId), CancellationToken.None);

        var hasAny = await repository.HasAnyForEventAsync(
            CalendarEventId,
            CancellationToken.None);

        Assert.True(hasAny);
        Assert.Equal(1, tableClient.LastQueryMaxPerPage);
        Assert.Equal(["PartitionKey"], tableClient.LastQuerySelect);
    }

    [Fact]
    public async Task ReleasePublishingAsync_ExistingRow_RemovesRow()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        await repository.StartPublishingAsync(Attempt(), CancellationToken.None);

        await repository.ReleasePublishingAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None);

        Assert.Empty(tableClient.Entities);
    }

    [Fact]
    public async Task ReleasePublishingAsync_MissingRow_Completes()
    {
        var repository = CreateRepository(new PlatformPublicationTableClient());

        await repository.ReleasePublishingAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None);
    }

    [Fact]
    public async Task MarkFailedAsync_PublishingRow_StoresFailedAndExternalId()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        await repository.StartPublishingAsync(Attempt(), CancellationToken.None);

        var result = await repository.MarkFailedAsync(
            CalendarEventId,
            PlatformId,
            " created-broadcast-id ",
            Failure(),
            CancellationToken.None);
        var publication = await repository.GetAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None);

        Assert.Equal(MarkFailedResult.Marked, result);
        Assert.Equal(PublishStatus.Failed, publication!.Status);
        Assert.Equal("created-broadcast-id", publication.ExternalResourceId);
        Assert.Null(publication.PublishedUtc);
        Assert.Equal("Rendered title", publication.ContentSnapshot!.Title);
        Assert.Equal(Failure(), publication.LastFailure);
    }

    [Fact]
    public async Task MarkFailedAsync_MissingRow_ReturnsNotFound()
    {
        var repository = CreateRepository(new PlatformPublicationTableClient());

        var result = await repository.MarkFailedAsync(
            CalendarEventId,
            PlatformId,
            externalResourceId: null,
            Failure(),
            CancellationToken.None);

        Assert.Equal(MarkFailedResult.NotFound, result);
    }

    [Fact]
    public async Task MarkFailedAsync_PublishedRow_ReturnsChanged()
    {
        var tableClient = new PlatformPublicationTableClient();
        tableClient.Seed(PublishedEntity(SchedulingSampleIds.YouTubeBroadcastId));
        var repository = CreateRepository(tableClient);

        var result = await repository.MarkFailedAsync(
            CalendarEventId,
            PlatformId,
            "other-id",
            Failure(),
            CancellationToken.None);

        Assert.Equal(MarkFailedResult.Changed, result);
        Assert.Equal(
            PublishStatus.Published.ToString(),
            Assert.Single(tableClient.Entities).Value.Status);
    }

    [Fact]
    public async Task MarkFailedAsync_ConcurrentChange_ReturnsChanged()
    {
        var tableClient = new PlatformPublicationTableClient();
        await CreateRepository(tableClient).StartPublishingAsync(Attempt(), CancellationToken.None);
        var stored = Assert.Single(tableClient.Entities).Value;
        tableClient.FailNextUpdateWithPreconditionFailed(stored);
        var repository = CreateRepository(tableClient);

        var result = await repository.MarkFailedAsync(
            CalendarEventId,
            PlatformId,
            externalResourceId: null,
            Failure(),
            CancellationToken.None);

        Assert.Equal(MarkFailedResult.Changed, result);
    }

    [Fact]
    public async Task StartPublishingAsync_FailedRow_ConditionallyStartsRetry()
    {
        var tableClient = new PlatformPublicationTableClient();
        tableClient.Seed(PublicationEntity(PublishStatus.Failed, externalResourceId: "old-id"));
        var repository = CreateRepository(tableClient);

        var first = await repository.StartPublishingAsync(Attempt(), CancellationToken.None);
        var second = await repository.StartPublishingAsync(Attempt(), CancellationToken.None);
        var publication = await repository.GetAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None);

        Assert.Equal(StartPublicationResult.Started, first);
        Assert.Equal(StartPublicationResult.Conflict, second);
        Assert.Equal(PublishStatus.Publishing, publication!.Status);
        Assert.Null(publication.ExternalResourceId);
    }

    [Fact]
    public async Task StartPublishingAsync_FailedRow_ClearsPreviousFailureDiagnostics()
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Failed, externalResourceId: null);
        entity.FailureCode = Failure().Code;
        entity.FailureMessage = Failure().Message;
        entity.FailureStage = Failure().Stage;
        entity.FailedUtc = Failure().FailedUtc;
        entity.FailureAttemptId = Failure().AttemptId;
        entity.FailureVerificationRequired = true;
        tableClient.Seed(entity);
        var repository = CreateRepository(tableClient);

        var result = await repository.StartPublishingAsync(
            Attempt() with { AttemptId = "retry-attempt-id" },
            CancellationToken.None);
        var publication = await repository.GetAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None);

        Assert.Equal(StartPublicationResult.Started, result);
        Assert.Equal(PublishStatus.Publishing, publication!.Status);
        Assert.Null(publication.LastFailure);
        Assert.Equal(
            "retry-attempt-id",
            Assert.Single(tableClient.Entities).Value.AttemptId);
    }

    [Fact]
    public async Task StartPublishingAsync_FailedRetryPreconditionChanged_ReturnsConflict()
    {
        var tableClient = new PlatformPublicationTableClient();
        var failed = PublicationEntity(PublishStatus.Failed, externalResourceId: "old-id");
        tableClient.Seed(failed);
        tableClient.FailNextUpdateWithPreconditionFailed(failed);
        var repository = CreateRepository(tableClient);

        var result = await repository.StartPublishingAsync(Attempt(), CancellationToken.None);

        Assert.Equal(StartPublicationResult.Conflict, result);
        Assert.Equal(PublishStatus.Failed.ToString(), Assert.Single(tableClient.Entities).Value.Status);
    }

    [Fact]
    public async Task DeletePublishedAsync_MatchingPublishedRow_DeletesAndReturnsDeleted()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        tableClient.Seed(PublishedEntity(SchedulingSampleIds.YouTubeBroadcastId));

        var result = await repository.DeletePublishedAsync(
            CalendarEventId,
            PlatformId,
            SchedulingSampleIds.YouTubeBroadcastId,
            CancellationToken.None);

        Assert.Equal(DeletePublishedResult.Deleted, result);
        Assert.Empty(tableClient.Entities);
    }

    [Fact]
    public async Task DeletePublishedAsync_MissingRow_ReturnsNotFound()
    {
        var repository = CreateRepository(new PlatformPublicationTableClient());

        var result = await repository.DeletePublishedAsync(
            CalendarEventId,
            PlatformId,
            SchedulingSampleIds.YouTubeBroadcastId,
            CancellationToken.None);

        Assert.Equal(DeletePublishedResult.NotFound, result);
    }

    [Fact]
    public async Task DeletePublishedAsync_PublishingRow_ReturnsChangedAndLeavesRow()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        var entity = PublishedEntity(SchedulingSampleIds.YouTubeBroadcastId);
        entity.Status = PublishStatus.Publishing.ToString();
        tableClient.Seed(entity);

        var result = await repository.DeletePublishedAsync(
            CalendarEventId,
            PlatformId,
            SchedulingSampleIds.YouTubeBroadcastId,
            CancellationToken.None);

        Assert.Equal(DeletePublishedResult.Changed, result);
        Assert.Single(tableClient.Entities);
    }

    [Fact]
    public async Task DeletePublishedAsync_ExternalResourceIdChanged_ReturnsChangedAndLeavesRow()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        tableClient.Seed(PublishedEntity("other-resource-id"));

        var result = await repository.DeletePublishedAsync(
            CalendarEventId,
            PlatformId,
            SchedulingSampleIds.YouTubeBroadcastId,
            CancellationToken.None);

        Assert.Equal(DeletePublishedResult.Changed, result);
        Assert.Single(tableClient.Entities);
    }

    [Fact]
    public async Task DeletePublishedAsync_OrphanedRow_ReturnsChangedAndLeavesRow()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        var entity = PublishedEntity(SchedulingSampleIds.YouTubeBroadcastId);
        entity.PlatformDeletedUtc = new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);
        tableClient.Seed(entity);

        var result = await repository.DeletePublishedAsync(
            CalendarEventId,
            PlatformId,
            SchedulingSampleIds.YouTubeBroadcastId,
            CancellationToken.None);

        Assert.Equal(DeletePublishedResult.Changed, result);
        Assert.Single(tableClient.Entities);
    }

    [Fact]
    public async Task DeletePublishedAsync_DeletePreconditionFailed_ReturnsChangedAndLeavesRow()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        var entity = PublishedEntity(SchedulingSampleIds.YouTubeBroadcastId);
        tableClient.Seed(entity);
        tableClient.FailDeleteWithPreconditionFailed(entity);

        var result = await repository.DeletePublishedAsync(
            CalendarEventId,
            PlatformId,
            SchedulingSampleIds.YouTubeBroadcastId,
            CancellationToken.None);

        Assert.Equal(DeletePublishedResult.Changed, result);
        Assert.Single(tableClient.Entities);
    }

    [Fact]
    public async Task HasPublishingByPlatformAsync_FiltersPublishingRowsForRequestedPlatform()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        tableClient.Seed(PublicationEntity(
            PublishStatus.Publishing,
            calendarEventId: CalendarEventId,
            platformId: PlatformId));
        tableClient.Seed(PublicationEntity(
            PublishStatus.Published,
            calendarEventId: OtherCalendarEventId,
            platformId: PlatformId));
        tableClient.Seed(PublicationEntity(
            PublishStatus.Publishing,
            calendarEventId: OtherCalendarEventId,
            platformId: "other-platform-id"));

        var result = await repository.HasPublishingByPlatformAsync(
            PlatformId,
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(1, tableClient.LastQueryMaxPerPage);
        Assert.Equal(["PartitionKey"], tableClient.LastQuerySelect);
    }

    [Fact]
    public async Task OrphanPublishedByPlatformAsync_StampsOnlyPublishedRowsForRequestedPlatform()
    {
        var tableClient = new PlatformPublicationTableClient();
        var repository = CreateRepository(tableClient);
        var published = PublicationEntity(
            PublishStatus.Published,
            calendarEventId: CalendarEventId,
            platformId: PlatformId);
        var publishing = PublicationEntity(
            PublishStatus.Publishing,
            calendarEventId: OtherCalendarEventId,
            platformId: PlatformId);
        var otherPlatformPublished = PublicationEntity(
            PublishStatus.Published,
            calendarEventId: OtherCalendarEventId,
            platformId: "other-platform-id");
        tableClient.Seed(published);
        tableClient.Seed(publishing);
        tableClient.Seed(otherPlatformPublished);

        var orphaned = await repository.OrphanPublishedByPlatformAsync(
            PlatformId,
            CancellationToken.None);

        Assert.Equal(1, orphaned);
        Assert.NotNull(tableClient.Entities[(published.PartitionKey, published.RowKey)].PlatformDeletedUtc);
        Assert.Null(tableClient.Entities[(publishing.PartitionKey, publishing.RowKey)].PlatformDeletedUtc);
        Assert.Null(tableClient.Entities[
            (otherPlatformPublished.PartitionKey, otherPlatformPublished.RowKey)].PlatformDeletedUtc);
    }

    [Fact]
    public void CanDeletePublished_PublishedRowWithMatchingExternalId_ReturnsDeleted()
    {
        var entity = PublishedEntity(SchedulingSampleIds.YouTubeBroadcastId);

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            SchedulingSampleIds.YouTubeBroadcastId);

        Assert.Equal(DeletePublishedResult.Deleted, result);
    }

    [Fact]
    public void CanDeletePublished_PublishingRow_ReturnsChanged()
    {
        var entity = PublishedEntity(SchedulingSampleIds.YouTubeBroadcastId);
        entity.Status = PublishStatus.Publishing.ToString();

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            SchedulingSampleIds.YouTubeBroadcastId);

        Assert.Equal(DeletePublishedResult.Changed, result);
    }

    [Fact]
    public void CanDeletePublished_OrphanedPublishedRow_ReturnsChanged()
    {
        var entity = PublishedEntity(SchedulingSampleIds.YouTubeBroadcastId);
        entity.PlatformDeletedUtc = new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            SchedulingSampleIds.YouTubeBroadcastId);

        Assert.Equal(DeletePublishedResult.Changed, result);
    }

    [Fact]
    public void CanDeletePublished_ExternalResourceIdChanged_ReturnsChanged()
    {
        var entity = PublishedEntity("other-resource-id");

        var result = AzurePlatformPublicationRepository.CanDeletePublished(
            entity,
            SchedulingSampleIds.YouTubeBroadcastId);

        Assert.Equal(DeletePublishedResult.Changed, result);
    }

    [Fact]
    public async Task SaveExternalResourceIdAsync_PublishingRow_ConditionallyStoresId()
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Publishing);
        entity.UpdatedUtc = entity.UpdatedUtc.AddMinutes(-1);
        tableClient.Seed(entity);
        var repository = CreateRepository(tableClient);

        var result = await repository.SaveExternalResourceIdAsync(
            CalendarEventId,
            PlatformId,
            " checkpoint-id ",
            CancellationToken.None);

        Assert.Equal(SaveExternalResourceIdResult.Saved, result);
        var stored = Assert.Single(tableClient.Entities).Value;
        Assert.Equal("checkpoint-id", stored.ExternalResourceId);
        Assert.Equal(new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero), stored.UpdatedUtc);
        Assert.Equal("Rendered title", stored.ContentSnapshotTitle);
        Assert.Equal(entity.PublishSettingsJson, stored.PublishSettingsJson);
    }

    [Fact]
    public async Task SaveExternalResourceIdAsync_MissingRow_ReturnsNotFound()
    {
        var repository = CreateRepository(new PlatformPublicationTableClient());

        var result = await repository.SaveExternalResourceIdAsync(
            CalendarEventId,
            PlatformId,
            "checkpoint-id",
            CancellationToken.None);

        Assert.Equal(SaveExternalResourceIdResult.NotFound, result);
    }

    [Theory]
    [InlineData(PublishStatus.Published)]
    [InlineData(PublishStatus.Failed)]
    public async Task SaveExternalResourceIdAsync_NonPublishingRow_ReturnsChanged(
        PublishStatus status)
    {
        var tableClient = new PlatformPublicationTableClient();
        tableClient.Seed(PublicationEntity(status));
        var repository = CreateRepository(tableClient);

        var result = await repository.SaveExternalResourceIdAsync(
            CalendarEventId,
            PlatformId,
            "checkpoint-id",
            CancellationToken.None);

        Assert.Equal(SaveExternalResourceIdResult.Changed, result);
    }

    [Fact]
    public async Task SaveExternalResourceIdAsync_OrphanPublishingRow_ReturnsChanged()
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Publishing);
        entity.PlatformDeletedUtc = entity.UpdatedUtc;
        tableClient.Seed(entity);

        var result = await CreateRepository(tableClient).SaveExternalResourceIdAsync(
            CalendarEventId,
            PlatformId,
            "checkpoint-id",
            CancellationToken.None);

        Assert.Equal(SaveExternalResourceIdResult.Changed, result);
    }

    [Fact]
    public async Task SaveExternalResourceIdAsync_EtagRace_ReturnsChanged()
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Publishing);
        tableClient.Seed(entity);
        tableClient.FailNextUpdateWithPreconditionFailed(entity);

        var result = await CreateRepository(tableClient).SaveExternalResourceIdAsync(
            CalendarEventId,
            PlatformId,
            "checkpoint-id",
            CancellationToken.None);

        Assert.Equal(SaveExternalResourceIdResult.Changed, result);
    }

    [Fact]
    public async Task SaveExternalResourceIdAsync_RepeatedSameId_RemainsSaved()
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Publishing);
        entity.ExternalResourceId = "checkpoint-id";
        tableClient.Seed(entity);

        var result = await CreateRepository(tableClient).SaveExternalResourceIdAsync(
            CalendarEventId,
            PlatformId,
            "checkpoint-id",
            CancellationToken.None);

        Assert.Equal(SaveExternalResourceIdResult.Saved, result);
        Assert.Equal("checkpoint-id", Assert.Single(tableClient.Entities).Value.ExternalResourceId);
    }

    [Fact]
    public async Task SaveExternalResourceIdAsync_ConflictingId_ReturnsChangedAndPreservesCheckpoint()
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Publishing);
        entity.ExternalResourceId = "first-id";
        tableClient.Seed(entity);

        var result = await CreateRepository(tableClient).SaveExternalResourceIdAsync(
            CalendarEventId,
            PlatformId,
            "different-id",
            CancellationToken.None);

        Assert.Equal(SaveExternalResourceIdResult.Changed, result);
        Assert.Equal("first-id", Assert.Single(tableClient.Entities).Value.ExternalResourceId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("different-id")]
    public async Task MarkFailedAsync_CheckpointExists_PreservesCheckpointAndSnapshots(
        string? failureId)
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Publishing);
        entity.ExternalResourceId = "checkpoint-id";
        entity.ThumbnailStatus = ThumbnailPublishStatus.Failed.ToString();
        tableClient.Seed(entity);

        var result = await CreateRepository(tableClient).MarkFailedAsync(
            CalendarEventId,
            PlatformId,
            failureId,
            Failure(),
            CancellationToken.None);

        Assert.Equal(MarkFailedResult.Marked, result);
        var stored = Assert.Single(tableClient.Entities).Value;
        Assert.Equal("checkpoint-id", stored.ExternalResourceId);
        Assert.Equal(PublishStatus.Failed.ToString(), stored.Status);
        Assert.Equal(entity.PublishSettingsJson, stored.PublishSettingsJson);
        Assert.Equal(entity.ContentSnapshotTitle, stored.ContentSnapshotTitle);
        Assert.Equal(entity.ContentSnapshotDescription, stored.ContentSnapshotDescription);
        Assert.Equal(ThumbnailPublishStatus.Failed.ToString(), stored.ThumbnailStatus);
    }

    [Fact]
    public async Task RecoverStalePublishingAsync_ObservedRow_ChangesOnlyStatusAndUpdatedUtc()
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Publishing);
        entity.ExternalResourceId = "checkpoint-id";
        entity.UpdatedUtc = entity.UpdatedUtc.AddMinutes(-10);
        var observedUpdatedUtc = entity.UpdatedUtc;
        tableClient.Seed(entity);

        var result = await CreateRepository(tableClient).RecoverStalePublishingAsync(
            CalendarEventId,
            PlatformId,
            observedUpdatedUtc,
            CancellationToken.None);

        Assert.Equal(RecoverStalePublishingResult.Recovered, result);
        var stored = Assert.Single(tableClient.Entities).Value;
        Assert.Equal(PublishStatus.Failed.ToString(), stored.Status);
        Assert.Equal("checkpoint-id", stored.ExternalResourceId);
        Assert.Equal(entity.CreatedUtc, stored.CreatedUtc);
        Assert.Equal(entity.PublishedUtc, stored.PublishedUtc);
        Assert.Equal(entity.PublishSettingsJson, stored.PublishSettingsJson);
        Assert.Equal(entity.ContentSnapshotTitle, stored.ContentSnapshotTitle);
    }

    [Fact]
    public async Task RecoverStalePublishingAsync_UpdatedUtcChanged_ReturnsChanged()
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Publishing);
        tableClient.Seed(entity);

        var result = await CreateRepository(tableClient).RecoverStalePublishingAsync(
            CalendarEventId,
            PlatformId,
            entity.UpdatedUtc.AddSeconds(-1),
            CancellationToken.None);

        Assert.Equal(RecoverStalePublishingResult.Changed, result);
    }

    [Fact]
    public async Task RecoverStalePublishingAsync_EtagRace_ReturnsChanged()
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Publishing);
        tableClient.Seed(entity);
        tableClient.FailNextUpdateWithPreconditionFailed(entity);

        var result = await CreateRepository(tableClient).RecoverStalePublishingAsync(
            CalendarEventId,
            PlatformId,
            entity.UpdatedUtc,
            CancellationToken.None);

        Assert.Equal(RecoverStalePublishingResult.Changed, result);
    }

    [Fact]
    public async Task RecoverStalePublishingAsync_RepeatedRecovery_ReturnsChanged()
    {
        var tableClient = new PlatformPublicationTableClient();
        var entity = PublicationEntity(PublishStatus.Publishing);
        tableClient.Seed(entity);
        var repository = CreateRepository(tableClient);

        var first = await repository.RecoverStalePublishingAsync(
            CalendarEventId,
            PlatformId,
            entity.UpdatedUtc,
            CancellationToken.None);
        var second = await repository.RecoverStalePublishingAsync(
            CalendarEventId,
            PlatformId,
            entity.UpdatedUtc,
            CancellationToken.None);

        Assert.Equal(RecoverStalePublishingResult.Recovered, first);
        Assert.Equal(RecoverStalePublishingResult.Changed, second);
    }

    private static AzurePlatformPublicationRepository CreateRepository(
        PlatformPublicationTableClient tableClient) =>
        new(tableClient, new FixedTimeProvider(
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)));

    private static async Task StartAndPublish(AzurePlatformPublicationRepository repository)
    {
        await repository.StartPublishingAsync(Attempt(), CancellationToken.None);
        await repository.MarkPublishedAsync(
            CalendarEventId,
            PlatformId,
            SchedulingSampleIds.YouTubeBroadcastId,
            CancellationToken.None);
    }

    private static PlatformPublicationAttempt Attempt(string calendarEventId = CalendarEventId) =>
        new(
            calendarEventId,
            PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            SchedulingSamples.YouTubeSettings(),
            new ContentSnapshot("Rendered title", "Rendered description"));

    private static PublicationFailure Failure() =>
        new(
            "provider_failure",
            "The provider failed.",
            "provider",
            ProviderStatus: null,
            ProviderErrorCode: null,
            RetryAfterUtc: null,
            FailedUtc: new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            AttemptId: "attempt-id",
            VerificationRequired: true);

    private static PlatformPublicationEntity PublishedEntity(string externalResourceId) =>
        PublicationEntity(PublishStatus.Published, externalResourceId: externalResourceId);

    private static PlatformPublicationEntity PublicationEntity(
        PublishStatus status,
        string calendarEventId = CalendarEventId,
        string platformId = PlatformId,
        string? externalResourceId = SchedulingSampleIds.YouTubeBroadcastId) =>
        new()
        {
            PartitionKey = PlatformPublicationKey.PartitionKeyFor(calendarEventId),
            RowKey = PlatformPublicationKey.RowKeyFor(platformId),
            CalendarEventId = calendarEventId,
            PlatformId = platformId,
            PlatformName = "Main YouTube channel",
            PlatformType = PlatformType.YouTube.ToString(),
            Status = status.ToString(),
            ExternalResourceId = status is PublishStatus.Published or PublishStatus.Failed
                ? externalResourceId
                : null,
            ThumbnailStatus = ThumbnailPublishStatus.NotConfigured.ToString(),
            ContentSnapshotTitle = "Rendered title",
            ContentSnapshotDescription = "Rendered description",
            PublishSettingsJson = PublishSettingsSerializer.SerializeSnapshot(
                PlatformType.YouTube,
                SchedulingSamples.YouTubeSettings()),
            PublishedUtc = status == PublishStatus.Published
                ? new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)
                : null,
            CreatedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            UpdatedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero)
        };

}

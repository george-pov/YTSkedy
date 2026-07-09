using YTSkedy.Infrastructure.Platforms;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Scheduling.Application.Platforms;
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
    public async Task ListPublishingByPlatformAsync_FiltersPublishingRowsForRequestedPlatform()
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

        var result = await repository.ListPublishingByPlatformAsync(
            PlatformId,
            CancellationToken.None);

        var publication = Assert.Single(result);
        Assert.Equal(CalendarEventId, publication.CalendarEventId);
        Assert.Equal(PlatformId, publication.PlatformId);
        Assert.Equal(PublishStatus.Publishing, publication.Status);
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
            ExternalResourceId = status == PublishStatus.Published ? externalResourceId : null,
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

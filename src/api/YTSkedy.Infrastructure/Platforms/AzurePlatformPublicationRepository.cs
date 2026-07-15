using Azure;
using Azure.Data.Tables;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Platforms;

/// <summary>
/// Azure Table-backed publication store implementing focused publication write
/// ports and the read port (<see cref="IPlatformPublicationReader"/>). Rows are
/// partitioned by calendar event (<c>event-{calendarEventId}</c>) and keyed by
/// platform (<c>platform-{platformId}</c>), so every publication for an event
/// reads from one partition. Starting an attempt uses a conditional insert so
/// two concurrent publish attempts cannot both start the same pair. Storage
/// identity, ETags, and id formatting stay inside this class.
/// </summary>
public sealed class AzurePlatformPublicationRepository(
    TableClient tableClient,
    TimeProvider timeProvider) :
    IPublicationAttemptWriter,
    IPublicationThumbnailWriter,
    IPublicationCleanupWriter,
    IPublicationHistoryWriter,
    IPlatformPublicationReader
{
    public async Task<StartPublicationResult> StartPublishingAsync(
        PlatformPublicationAttempt attempt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        var entity = PlatformPublicationMapper.ToPublishingEntity(
            attempt,
            timeProvider.GetUtcNow());

        try
        {
            // Conditional insert: AddEntity fails with 409 when a row already
            // exists for the pair. An active Failed row may then be replaced
            // conditionally; every other state remains a conflict.
            await tableClient.AddEntityAsync(entity, cancellationToken);

            return StartPublicationResult.Started;
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            return await TryRestartFailedAsync(entity, cancellationToken);
        }
    }

    private async Task<StartPublicationResult> TryRestartFailedAsync(
        PlatformPublicationEntity retryEntity,
        CancellationToken cancellationToken)
    {
        var existing = await TryGetEntityAsync(
            retryEntity.CalendarEventId,
            retryEntity.PlatformId,
            cancellationToken);
        if (existing is null ||
            existing.PlatformDeletedUtc is not null ||
            PlatformPublicationMapper.ParseStatus(existing.Status) != PublishStatus.Failed)
        {
            return StartPublicationResult.Conflict;
        }

        retryEntity.ETag = existing.ETag;
        retryEntity.CreatedUtc = existing.CreatedUtc;

        try
        {
            await tableClient.UpdateEntityAsync(
                retryEntity,
                existing.ETag,
                TableUpdateMode.Replace,
                cancellationToken);

            return StartPublicationResult.Started;
        }
        catch (RequestFailedException exception) when (exception.Status is 404 or 412)
        {
            return StartPublicationResult.Conflict;
        }
    }

    public async Task<SaveExternalResourceIdResult> SaveExternalResourceIdAsync(
        string calendarEventId,
        string platformId,
        string externalResourceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalResourceId);

        var entity = await TryGetEntityAsync(calendarEventId, platformId, cancellationToken);
        if (entity is null)
        {
            return SaveExternalResourceIdResult.NotFound;
        }

        var normalizedExternalResourceId = externalResourceId.Trim();
        if (entity.PlatformDeletedUtc is not null ||
            PlatformPublicationMapper.ParseStatus(entity.Status) != PublishStatus.Publishing ||
            (!string.IsNullOrWhiteSpace(entity.ExternalResourceId) &&
             !string.Equals(
                 entity.ExternalResourceId,
                 normalizedExternalResourceId,
                 StringComparison.Ordinal)))
        {
            return SaveExternalResourceIdResult.Changed;
        }

        entity.ExternalResourceId = normalizedExternalResourceId;
        entity.UpdatedUtc = timeProvider.GetUtcNow();

        try
        {
            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken);

            return SaveExternalResourceIdResult.Saved;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return SaveExternalResourceIdResult.NotFound;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            return SaveExternalResourceIdResult.Changed;
        }
    }

    public async Task<RecoverStalePublishingResult> RecoverStalePublishingAsync(
        string calendarEventId,
        string platformId,
        DateTimeOffset expectedUpdatedUtc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        var entity = await TryGetEntityAsync(calendarEventId, platformId, cancellationToken);
        if (entity is null)
        {
            return RecoverStalePublishingResult.NotFound;
        }

        if (entity.PlatformDeletedUtc is not null ||
            PlatformPublicationMapper.ParseStatus(entity.Status) != PublishStatus.Publishing ||
            entity.UpdatedUtc != expectedUpdatedUtc)
        {
            return RecoverStalePublishingResult.Changed;
        }

        entity.Status = PublishStatus.Failed.ToString();
        entity.UpdatedUtc = timeProvider.GetUtcNow();

        try
        {
            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken);
            return RecoverStalePublishingResult.Recovered;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return RecoverStalePublishingResult.NotFound;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            return RecoverStalePublishingResult.Changed;
        }
    }

    public async Task ReleasePublishingAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        try
        {
            var entity = await TryGetEntityAsync(
                calendarEventId,
                platformId,
                cancellationToken);
            if (entity is null ||
                PlatformPublicationMapper.ParseStatus(entity.Status) != PublishStatus.Publishing)
            {
                return;
            }

            await tableClient.DeleteEntityAsync(
                entity.PartitionKey,
                entity.RowKey,
                entity.ETag,
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status is 404 or 412)
        {
            // The row is already gone, which is the intended end state.
        }
    }

    public async Task<DateTimeOffset?> MarkPublishedAsync(
        string calendarEventId,
        string platformId,
        string externalResourceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalResourceId);

        var entity = await TryGetEntityAsync(calendarEventId, platformId, cancellationToken);

        if (entity is null ||
            entity.PlatformDeletedUtc is not null ||
            PlatformPublicationMapper.ParseStatus(entity.Status) != PublishStatus.Publishing)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        entity.Status = PublishStatus.Published.ToString();
        entity.ExternalResourceId = externalResourceId;
        entity.PublishedUtc = now;
        entity.UpdatedUtc = now;

        try
        {
            // Conditional on the read ETag so the finalize cannot overwrite a
            // concurrent change to the started row.
            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken);

            return now;
        }
        catch (RequestFailedException exception) when (exception.Status is 404 or 412)
        {
            // The row was removed between the read and this write.
            return null;
        }
    }

    public async Task<MarkFailedResult> MarkFailedAsync(
        string calendarEventId,
        string platformId,
        string? externalResourceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        var entity = await TryGetEntityAsync(calendarEventId, platformId, cancellationToken);
        if (entity is null)
        {
            return MarkFailedResult.NotFound;
        }

        if (entity.PlatformDeletedUtc is not null ||
            PlatformPublicationMapper.ParseStatus(entity.Status) != PublishStatus.Publishing)
        {
            return MarkFailedResult.Changed;
        }

        var now = timeProvider.GetUtcNow();
        entity.Status = PublishStatus.Failed.ToString();
        var checkpointedExternalResourceId = string.IsNullOrWhiteSpace(entity.ExternalResourceId)
            ? null
            : entity.ExternalResourceId.Trim();
        var failureExternalResourceId = string.IsNullOrWhiteSpace(externalResourceId)
            ? null
            : externalResourceId.Trim();
        entity.ExternalResourceId = checkpointedExternalResourceId ?? failureExternalResourceId;
        entity.PublishedUtc = null;
        entity.UpdatedUtc = now;

        try
        {
            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken);

            return MarkFailedResult.Marked;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return MarkFailedResult.NotFound;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            return MarkFailedResult.Changed;
        }
    }

    public Task<bool> MarkThumbnailAppliedAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken) =>
        MarkThumbnailStatusAsync(
            calendarEventId,
            platformId,
            ThumbnailPublishStatus.Applied,
            cancellationToken);

    public Task<bool> MarkThumbnailFailedAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken) =>
        MarkThumbnailStatusAsync(
            calendarEventId,
            platformId,
            ThumbnailPublishStatus.Failed,
            cancellationToken);

    public async Task<DeletePublishedResult> DeletePublishedAsync(
        string calendarEventId,
        string platformId,
        string externalResourceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalResourceId);

        var entity = await TryGetEntityAsync(calendarEventId, platformId, cancellationToken);

        if (entity is null)
        {
            return DeletePublishedResult.NotFound;
        }

        var eligibility = CanDeletePublished(entity, externalResourceId);
        if (eligibility != DeletePublishedResult.Deleted)
        {
            return eligibility;
        }

        try
        {
            await tableClient.DeleteEntityAsync(
                entity.PartitionKey,
                entity.RowKey,
                entity.ETag,
                cancellationToken);

            return DeletePublishedResult.Deleted;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return DeletePublishedResult.NotFound;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            return DeletePublishedResult.Changed;
        }
    }

    private async Task<bool> MarkThumbnailStatusAsync(
        string calendarEventId,
        string platformId,
        ThumbnailPublishStatus thumbnailStatus,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        var entity = await TryGetEntityAsync(calendarEventId, platformId, cancellationToken);

        if (entity is null || !CanMarkThumbnailStatus(entity))
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        entity.ThumbnailStatus = thumbnailStatus.ToString();
        entity.UpdatedUtc = now;

        try
        {
            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken);

            return true;
        }
        catch (RequestFailedException exception) when (exception.Status is 404 or 412)
        {
            return false;
        }
    }

    internal static bool CanMarkThumbnailStatus(PlatformPublicationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return PlatformPublicationMapper.ParseStatus(entity.Status) == PublishStatus.Published;
    }

    internal static DeletePublishedResult CanDeletePublished(
        PlatformPublicationEntity entity,
        string externalResourceId)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalResourceId);

        var status = PlatformPublicationMapper.ParseStatus(entity.Status);
        return status == PublishStatus.Published &&
            entity.PlatformDeletedUtc is null &&
            string.Equals(entity.ExternalResourceId, externalResourceId, StringComparison.Ordinal)
                ? DeletePublishedResult.Deleted
                : DeletePublishedResult.Changed;
    }

    public async Task<int> OrphanPublishedByPlatformAsync(
        string platformId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        var now = timeProvider.GetUtcNow();
        var orphaned = 0;

        foreach (var entity in await QueryAsync(PublishedByPlatformFilter(platformId), cancellationToken))
        {
            entity.PlatformDeletedUtc = now;
            entity.UpdatedUtc = now;

            try
            {
                await tableClient.UpdateEntityAsync(
                    entity,
                    entity.ETag,
                    TableUpdateMode.Replace,
                    cancellationToken);

                orphaned++;
            }
            catch (RequestFailedException exception) when (exception.Status is 404 or 412)
            {
                // The row changed or was removed after it was read; skip it and
                // leave its state to the writer that changed it.
            }
        }

        return orphaned;
    }

    public async Task<IReadOnlyList<PlatformPublication>> ListByEventAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var entities = await QueryAsync(EventPartitionFilter(calendarEventId), cancellationToken);

        return PlatformPublicationMapper.ToPublications(entities);
    }

    public async Task<bool> HasAnyForEventAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        try
        {
            await foreach (var _ in tableClient.QueryAsync<PlatformPublicationEntity>(
                EventPartitionFilter(calendarEventId),
                cancellationToken: cancellationToken))
            {
                return true;
            }
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return false;
        }

        return false;
    }

    public async Task<PlatformPublication?> GetAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        var entity = await TryGetEntityAsync(calendarEventId, platformId, cancellationToken);

        return entity is null ? null : PlatformPublicationMapper.ToPublication(entity);
    }

    public async Task<IReadOnlyList<PlatformPublication>> ListPublishingByPlatformAsync(
        string platformId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        var entities = await QueryAsync(PublishingByPlatformFilter(platformId), cancellationToken);

        return PlatformPublicationMapper.ToPublications(entities);
    }

    private async Task<PlatformPublicationEntity?> TryGetEntityAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await tableClient.GetEntityIfExistsAsync<PlatformPublicationEntity>(
                PlatformPublicationKey.PartitionKeyFor(calendarEventId),
                PlatformPublicationKey.RowKeyFor(platformId),
                cancellationToken: cancellationToken);

            return response.HasValue ? response.Value : null;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The table does not exist yet, so there is no publication to return.
            return null;
        }
    }

    private async Task<List<PlatformPublicationEntity>> QueryAsync(
        string filter,
        CancellationToken cancellationToken)
    {
        var entities = new List<PlatformPublicationEntity>();

        try
        {
            await foreach (var entity in tableClient.QueryAsync<PlatformPublicationEntity>(
                filter,
                cancellationToken: cancellationToken))
            {
                entities.Add(entity);
            }
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The table does not exist yet, so there are no publications to return.
        }

        return entities;
    }

    private static string EventPartitionFilter(string calendarEventId) =>
        $"PartitionKey eq '{PlatformPublicationKey.EscapeLiteral(
            PlatformPublicationKey.PartitionKeyFor(calendarEventId))}'";

    // Platform ids are server-generated hex GUIDs and the status values are
    // controlled constants, so these filter literals carry no caller input.
    private static string PublishingByPlatformFilter(string platformId) =>
        $"PlatformId eq '{PlatformPublicationKey.EscapeLiteral(platformId)}' and " +
        $"Status eq '{PublishStatus.Publishing}'";

    private static string PublishedByPlatformFilter(string platformId) =>
        $"PlatformId eq '{PlatformPublicationKey.EscapeLiteral(platformId)}' and " +
        $"Status eq '{PublishStatus.Published}'";
}

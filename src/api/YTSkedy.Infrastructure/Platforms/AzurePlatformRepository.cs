using Azure;
using Azure.Data.Tables;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Platforms;

/// <summary>
/// Azure Table-backed platform store implementing the write port
/// (<see cref="IPlatformModifier"/>) and the read port
/// (<see cref="IPlatformReader"/>). All platforms share one partition
/// (<c>platforms</c>) and the row key is <c>platform-{platformId}</c>, where the
/// platform id is a server-generated GUID. Name uniqueness is global and
/// enforced check-then-write against the single partition with an ordinal
/// comparison. Non-empty reference-key uniqueness uses the same partition scan
/// with case-insensitive comparison; duplicate races are resolved by the last
/// writer that reaches storage. Publish settings are stored as JSON. Storage
/// identity, id generation, and ETags stay inside this class.
/// </summary>
public sealed class AzurePlatformRepository(
    TableClient tableClient,
    TimeProvider timeProvider) :
    IPlatformModifier,
    IPlatformReader
{
    private const string PartitionKey = "platforms";

    public async Task<CreatePlatformResult> CreateAsync(
        Platform platform,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(platform);

        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        // Check-then-write global name uniqueness against the single partition.
        // The small expected number of platforms is why this store does not use
        // a dedicated uniqueness index row.
        var existing = await QueryPartitionAsync(cancellationToken);

        if (existing.Any(entity => NameEquals(entity.Name, platform.Name)))
        {
            return CreatePlatformResult.NameAlreadyExists();
        }

        if (HasReferenceKeyConflict(existing, currentRowKey: string.Empty, platform.ReferenceKey))
        {
            return CreatePlatformResult.ReferenceKeyAlreadyExists();
        }

        var id = Guid.NewGuid().ToString("N");
        var now = timeProvider.GetUtcNow();
        var entity = new PlatformEntity
        {
            PartitionKey = PartitionKey,
            RowKey = RowKeyFor(id),
            PlatformId = id,
            Name = platform.Name,
            ReferenceKey = platform.ReferenceKey,
            Type = platform.Type.ToString(),
            TitleTemplateId = platform.PublishingContent.TitleTemplateId,
            DescriptionTemplateId = platform.PublishingContent.DescriptionTemplateId,
            PublishSettingsJson = PublishSettingsSerializer.Serialize(
                platform.Type,
                platform.PublishSettings),
            CreatedUtc = now,
            UpdatedUtc = now
        };

        await tableClient.AddEntityAsync(entity, cancellationToken);

        return CreatePlatformResult.Created(id);
    }

    public async Task<UpdatePlatformResult> UpdateAsync(
        string platformId,
        string name,
        string? referenceKey,
        PublishSettings publishSettings,
        PublishingContent publishingContent,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(publishSettings);
        ArgumentNullException.ThrowIfNull(publishingContent);

        var rowKey = RowKeyFor(platformId);

        // One partition read serves both the locate and the rename uniqueness
        // check; the target row and its siblings come from the same snapshot.
        var partition = await QueryPartitionAsync(cancellationToken);

        var entity = partition.FirstOrDefault(
            candidate => string.Equals(candidate.RowKey, rowKey, StringComparison.Ordinal));

        if (entity is null)
        {
            return UpdatePlatformResult.NotFound;
        }

        var trimmedName = name.Trim();

        if (partition.Any(other =>
                !string.Equals(other.RowKey, rowKey, StringComparison.Ordinal) &&
                NameEquals(other.Name, trimmedName)))
        {
            return UpdatePlatformResult.NameAlreadyExists;
        }

        var normalizedReferenceKey = Platform.NormalizeReferenceKey(referenceKey);
        if (HasReferenceKeyConflict(partition, rowKey, normalizedReferenceKey))
        {
            return UpdatePlatformResult.ReferenceKeyAlreadyExists;
        }

        // Type is immutable, so it is read from the stored row and reused to
        // serialize the new settings.
        var type = PlatformViewMapper.ParseType(entity.Type);
        entity.Name = trimmedName;
        entity.ReferenceKey = normalizedReferenceKey;
        entity.TitleTemplateId = publishingContent.TitleTemplateId;
        entity.DescriptionTemplateId = publishingContent.DescriptionTemplateId;
        entity.PublishSettingsJson = PublishSettingsSerializer.Serialize(
            type,
            publishSettings);
        entity.UpdatedUtc = timeProvider.GetUtcNow();

        try
        {
            await tableClient.UpdateEntityAsync(
                entity,
                ETag.All,
                TableUpdateMode.Replace,
                cancellationToken);

            return UpdatePlatformResult.Updated;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The row was removed between the read and this write.
            return UpdatePlatformResult.NotFound;
        }
    }

    public async Task<DeletePlatformResult> DeleteAsync(
        string platformId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        try
        {
            await tableClient.DeleteEntityAsync(
                PartitionKey,
                RowKeyFor(platformId),
                ETag.All,
                cancellationToken);

            return DeletePlatformResult.Deleted;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return DeletePlatformResult.NotFound;
        }
    }

    public async Task<IReadOnlyList<PlatformView>> ListAsync(
        PlatformType? type,
        CancellationToken cancellationToken)
    {
        var entities = await QueryPartitionAsync(cancellationToken);

        var candidates = type is null
            ? entities
            : entities
                .Where(entity => PlatformViewMapper.ParseType(entity.Type) == type.Value)
                .ToList();

            return PlatformViewMapper.ToViews(candidates);
    }

    public async Task<IReadOnlySet<string>> ListIdsAsync(
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            await foreach (var entity in tableClient.QueryAsync<PlatformEntity>(
                PartitionFilter(),
                select: [nameof(PlatformEntity.PlatformId)],
                cancellationToken: cancellationToken))
            {
                ids.Add(entity.PlatformId);
            }
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The table does not exist yet, so there are no active platform ids.
        }

        return ids;
    }

    public async Task<PlatformView?> GetAsync(
        string platformId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        try
        {
            var response = await tableClient.GetEntityIfExistsAsync<PlatformEntity>(
                PartitionKey,
                RowKeyFor(platformId),
                cancellationToken: cancellationToken);

            return response.HasValue
                ? PlatformViewMapper.ToView(response.Value!)
                : null;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The table does not exist yet, so there is no platform to return.
            return null;
        }
    }

    private async Task<List<PlatformEntity>> QueryPartitionAsync(CancellationToken cancellationToken)
    {
        var entities = new List<PlatformEntity>();

        try
        {
            await foreach (var entity in tableClient.QueryAsync<PlatformEntity>(
                PartitionFilter(),
                cancellationToken: cancellationToken))
            {
                entities.Add(entity);
            }
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // The table does not exist yet, so there are no platforms to return.
        }

        return entities;
    }

    // The partition key is a controlled constant value, so it is safe to embed
    // directly in the filter.
    private static string PartitionFilter() =>
        $"PartitionKey eq '{PartitionKey}'";

    private static string RowKeyFor(string platformId) =>
        $"platform-{platformId}";

    private static bool NameEquals(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);

    private static bool ReferenceKeyEquals(string? left, string? right)
    {
        var normalizedLeft = Platform.NormalizeReferenceKey(left);
        var normalizedRight = Platform.NormalizeReferenceKey(right);

        return normalizedLeft is not null &&
            normalizedRight is not null &&
            string.Equals(
                Platform.ToReferenceKeyLookupValue(normalizedLeft),
                Platform.ToReferenceKeyLookupValue(normalizedRight),
                StringComparison.Ordinal);
    }

    private static bool HasReferenceKeyConflict(
        IEnumerable<PlatformEntity> entities,
        string currentRowKey,
        string? referenceKey)
    {
        var normalizedReferenceKey = Platform.NormalizeReferenceKey(referenceKey);
        if (normalizedReferenceKey is null)
        {
            return false;
        }

        return entities.Any(entity =>
            !string.Equals(entity.RowKey, currentRowKey, StringComparison.Ordinal) &&
            ReferenceKeyEquals(entity.ReferenceKey, normalizedReferenceKey));
    }
}

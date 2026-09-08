using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Platforms;

/// <summary>
/// Translates between <see cref="PlatformPublicationEntity"/> rows and the
/// domain <see cref="PlatformPublication"/> read model, and builds the row for
/// a fresh attempt. Status round-trips as its enum name; an
/// unparsable stored status fails the read. Publish settings snapshots are
/// serialized without secret material through
/// <see cref="PublishSettingsSerializer.SerializeSnapshot(PlatformType, PublishSettings)"/>.
/// </summary>
internal static class PlatformPublicationMapper
{
    internal static PlatformPublication ToPublication(PlatformPublicationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var platformType = PlatformViewMapper.ParseType(entity.PlatformType);

        return new PlatformPublication(
            entity.CalendarEventId,
            entity.PlatformId,
            entity.PlatformName,
            platformType,
            ParseStatus(entity.Status),
            entity.ExternalResourceId,
            entity.PublishedUtc,
            entity.PlatformDeletedUtc,
            entity.UpdatedUtc,
            PublishSettingsSerializer.DeserializeSnapshot(
                platformType,
                entity.PublishSettingsJson),
            ToContentSnapshot(entity),
            ParseThumbnailStatus(entity.ThumbnailStatus),
            ToFailure(entity));
    }

    internal static IReadOnlyList<PlatformPublication> ToPublications(
        IEnumerable<PlatformPublicationEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities
            .Select(ToPublication)
            .ToArray();
    }

    /// <summary>
    /// Builds the row for a new in-progress attempt. The status is fixed to
    /// <see cref="PublishStatus.Publishing"/> and the platform name, type, and
    /// publish settings are copied from the attempt so the attempt is
    /// described by the settings in effect when it started.
    /// </summary>
    internal static PlatformPublicationEntity ToPublishingEntity(
        PlatformPublicationAttempt attempt,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        return new PlatformPublicationEntity
        {
            PartitionKey = PlatformPublicationKey.PartitionKeyFor(attempt.CalendarEventId),
            RowKey = PlatformPublicationKey.RowKeyFor(attempt.PlatformId),
            CalendarEventId = attempt.CalendarEventId,
            PlatformId = attempt.PlatformId,
            PlatformName = attempt.PlatformName,
            PlatformType = attempt.PlatformType.ToString(),
            Status = PublishStatus.Publishing.ToString(),
            ExternalResourceId = null,
            ThumbnailStatus = ThumbnailPublicationPolicy
                .InitialStatusFor(attempt.PlatformType)?
                .ToString(),
            ContentSnapshotTitle = attempt.ContentSnapshot.Title,
            ContentSnapshotDescription = attempt.ContentSnapshot.Description,
            AttemptId = Normalize(attempt.AttemptId),
            PublishSettingsJson = PublishSettingsSerializer.SerializeSnapshot(
                attempt.PlatformType,
                attempt.PublishSettings),
            PublishedUtc = null,
            PlatformDeletedUtc = null,
            CreatedUtc = now,
            UpdatedUtc = now
        };
    }

    internal static PublishStatus ParseStatus(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "notpublished" => PublishStatus.NotPublished,
            "publishing" => PublishStatus.Publishing,
            "published" => PublishStatus.Published,
            "failed" => PublishStatus.Failed,
            _ => throw InvalidStoredValue(nameof(PublishStatus), status)
        };

    internal static ThumbnailPublishStatus? ParseThumbnailStatus(string? status) =>
        status?.ToLowerInvariant() switch
        {
            null or "" => null,
            "notconfigured" => ThumbnailPublishStatus.NotConfigured,
            "applied" => ThumbnailPublishStatus.Applied,
            "failed" => ThumbnailPublishStatus.Failed,
            _ => throw InvalidStoredValue(nameof(ThumbnailPublishStatus), status)
        };

    private static ContentSnapshot? ToContentSnapshot(PlatformPublicationEntity entity) =>
        entity.ContentSnapshotTitle is null
            ? null
            : new ContentSnapshot(
                entity.ContentSnapshotTitle,
                entity.ContentSnapshotDescription);

    private static PublicationFailure? ToFailure(PlatformPublicationEntity entity)
    {
        if (ParseStatus(entity.Status) != PublishStatus.Failed ||
            string.IsNullOrWhiteSpace(entity.FailureCode) ||
            string.IsNullOrWhiteSpace(entity.FailureMessage) ||
            string.IsNullOrWhiteSpace(entity.FailureStage) ||
            entity.FailedUtc is null)
        {
            return null;
        }

        return new PublicationFailure(
            entity.FailureCode.Trim(),
            entity.FailureMessage.Trim(),
            entity.FailureStage.Trim(),
            entity.FailureProviderStatus,
            Normalize(entity.FailureProviderErrorCode),
            entity.FailureRetryAfterUtc,
            entity.FailedUtc.Value,
            Normalize(entity.FailureAttemptId),
            entity.FailureVerificationRequired ?? true);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static InvalidOperationException InvalidStoredValue(string fieldName, string? value) =>
        new($"Stored {fieldName} value '{value ?? "<null>"}' is invalid.");
}

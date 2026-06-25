using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Platforms;

/// <summary>
/// Translates between <see cref="PlatformPublicationEntity"/> rows and the
/// domain <see cref="PlatformPublication"/> read model, and builds the row for
/// a fresh attempt. Status round-trips as its enum name; an
/// unparsable stored status is read defensively as
/// <see cref="PublishStatus.NotPublished"/>. Publish settings snapshots are
/// serialized without secret material through
/// <see cref="PublishSettingsSerializer.SerializeSnapshot(PlatformType, PublishSettings)"/>.
/// </summary>
internal static class PlatformPublicationMapper
{
    internal static PlatformPublication ToPublication(PlatformPublicationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PlatformPublication(
            entity.CalendarEventId,
            entity.PlatformId,
            entity.PlatformName,
            PlatformViewMapper.ParseType(entity.PlatformType),
            ParseStatus(entity.Status),
            entity.ExternalResourceId,
            entity.PublishedUtc,
            entity.PlatformDeletedUtc,
            entity.UpdatedUtc);
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
        Enum.TryParse<PublishStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : PublishStatus.NotPublished;
}

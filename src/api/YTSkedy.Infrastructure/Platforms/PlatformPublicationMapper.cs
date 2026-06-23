using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Platforms;

/// <summary>
/// Translates between <see cref="PlatformPublicationEntity"/> rows and the
/// domain <see cref="PlatformPublication"/> read model, and builds the row that
/// represents a fresh reservation. Status round-trips as its enum name; an
/// unparsable stored status is read defensively as
/// <see cref="PublishStatus.NotPublished"/>. Publish settings are serialized
/// without secret material through <see cref="PublishSettingsSerializer"/>.
/// </summary>
internal static class PlatformPublicationMapper
{
    public static PlatformPublication ToPublication(PlatformPublicationEntity entity)
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

    public static IReadOnlyList<PlatformPublication> ToPublications(
        IEnumerable<PlatformPublicationEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities
            .Select(ToPublication)
            .ToArray();
    }

    /// <summary>
    /// Builds the row for a new reservation. The status is fixed to
    /// <see cref="PublishStatus.Publishing"/> and the platform name, type, and
    /// publish settings are copied from the reservation so the attempt is
    /// described by the settings in effect when it started.
    /// </summary>
    public static PlatformPublicationEntity ToReservedEntity(
        PlatformPublicationReservation reservation,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        return new PlatformPublicationEntity
        {
            PartitionKey = PlatformPublicationKey.PartitionKeyFor(reservation.CalendarEventId),
            RowKey = PlatformPublicationKey.RowKeyFor(reservation.PlatformId),
            CalendarEventId = reservation.CalendarEventId,
            PlatformId = reservation.PlatformId,
            PlatformName = reservation.PlatformName,
            PlatformType = reservation.PlatformType.ToString(),
            Status = PublishStatus.Publishing.ToString(),
            ExternalResourceId = null,
            PublishSettingsJson = PublishSettingsSerializer.Serialize(
                reservation.PlatformType,
                reservation.PublishSettings),
            PublishedUtc = null,
            PlatformDeletedUtc = null,
            CreatedUtc = now,
            UpdatedUtc = now
        };
    }

    public static PublishStatus ParseStatus(string? status) =>
        Enum.TryParse<PublishStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : PublishStatus.NotPublished;
}

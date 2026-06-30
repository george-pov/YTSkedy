using Azure;
using Azure.Data.Tables;

namespace YTSkedy.Infrastructure.Platforms;

/// <summary>
/// Azure Table row for a platform publication. The partition key groups every
/// publication for one calendar event (<c>event-{calendarEventId}</c>) and the
/// row key identifies the platform (<c>platform-{platformId}</c>), so an
/// event/platform pair addresses exactly one row. The platform name, type, and
/// publish settings are copied onto the row so the attempt remains describable
/// after the platform record changes or is deleted. Only non-secret publish
/// settings are stored in <see cref="PublishSettingsJson"/>.
/// </summary>
internal sealed class PlatformPublicationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public string CalendarEventId { get; set; } = string.Empty;

    public string PlatformId { get; set; } = string.Empty;

    public string PlatformName { get; set; } = string.Empty;

    public string PlatformType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? ExternalResourceId { get; set; }

    public string? ContentSnapshotTitle { get; set; }

    public string? ContentSnapshotDescription { get; set; }

    public string PublishSettingsJson { get; set; } = string.Empty;

    public DateTimeOffset? PublishedUtc { get; set; }

    public DateTimeOffset? PlatformDeletedUtc { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}

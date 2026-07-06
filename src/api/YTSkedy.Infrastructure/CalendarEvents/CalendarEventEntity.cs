using Azure;
using Azure.Data.Tables;

namespace YTSkedy.Infrastructure.CalendarEvents;

internal sealed class CalendarEventEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public string CalendarEventId { get; set; } = string.Empty;

    public DateTimeOffset ScheduledStartUtc { get; set; }

    public string LocalDateTime { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public string TextJson { get; set; } = string.Empty;

    public string? ThumbnailJson { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }
}

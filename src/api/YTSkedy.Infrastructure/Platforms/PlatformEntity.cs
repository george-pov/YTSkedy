using Azure;
using Azure.Data.Tables;

namespace YTSkedy.Infrastructure.Platforms;

internal sealed class PlatformEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public string PlatformId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ReferenceKey { get; set; }

    public string Type { get; set; } = string.Empty;

    public string PublishSettingsJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}

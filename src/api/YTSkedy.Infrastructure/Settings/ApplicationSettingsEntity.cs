using Azure;
using Azure.Data.Tables;

namespace YTSkedy.Infrastructure.Settings;

internal sealed class ApplicationSettingsEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public string ValueJson { get; set; } = string.Empty;
}

using Azure;
using Azure.Data.Tables;

namespace YTSkedy.Infrastructure.Templates;

internal sealed class TemplateEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public string TemplateId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }
}

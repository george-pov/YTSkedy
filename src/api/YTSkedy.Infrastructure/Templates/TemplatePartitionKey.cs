using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Infrastructure.Templates;

/// <summary>
/// Owns the Azure Table partition-key scheme for templates. A template is
/// partitioned by its <see cref="TemplateType"/>, so every template of one type
/// shares a partition. That keeps name-uniqueness checks and type-scoped lists
/// to a single-partition query, and it is why the type is immutable once a
/// template is created.
/// </summary>
internal static class TemplatePartitionKey
{
    public static string ForType(TemplateType type) =>
        type switch
        {
            TemplateType.YouTube => "templates-youtube",
            TemplateType.WordPress => "templates-wordpress",
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unknown template type.")
        };
}

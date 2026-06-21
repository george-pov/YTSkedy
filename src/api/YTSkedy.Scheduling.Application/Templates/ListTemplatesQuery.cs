using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Template list query. <see cref="Type"/> is optional; when set the read is
/// scoped to that type, otherwise templates of every type are candidates.
/// </summary>
public sealed record ListTemplatesQuery(
    TemplateType? Type);

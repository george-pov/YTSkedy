using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

public sealed record UpdateTemplateCommand(
    TemplateType Type,
    string Id,
    string Name,
    string Content);

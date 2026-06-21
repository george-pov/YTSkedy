using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

public sealed record CreateTemplateCommand(
    string Name,
    TemplateType Type,
    string Content);

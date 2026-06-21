using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

public sealed record DeleteTemplateCommand(
    TemplateType Type,
    string Id);

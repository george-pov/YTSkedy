using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Returns the code-defined template token catalog. The handler is a thin
/// pass-through over <see cref="TemplateTokenCatalog.All"/> so the HTTP host has
/// a single use-case entry point and the token list stays defined in one place
/// in the domain. It performs no I/O, so it is synchronous.
/// </summary>
public sealed class ListTemplateTokensHandler
{
    public IReadOnlyList<TemplateToken> Handle() => TemplateTokenCatalog.All;
}

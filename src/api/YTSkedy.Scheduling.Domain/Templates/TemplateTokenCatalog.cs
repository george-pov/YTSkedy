namespace YTSkedy.Scheduling.Domain.Templates;

/// <summary>
/// Single source of truth for the placeholder tokens available to template
/// content. The catalog is code-defined rather than stored as data, and the
/// <c>template-tokens</c> endpoint reflects <see cref="All"/> without
/// duplicating the list. The set is expected to grow as more event data becomes
/// renderable.
/// </summary>
public static class TemplateTokenCatalog
{
    public static IReadOnlyList<TemplateToken> All { get; } =
    [
        new TemplateToken("localizedDate"),
        new TemplateToken("localizedTime"),
        new TemplateToken("youTubeBroadcastId"),
        new TemplateToken("calendarEventTitle")
    ];
}

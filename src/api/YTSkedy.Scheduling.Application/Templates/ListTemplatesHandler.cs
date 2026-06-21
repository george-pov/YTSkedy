using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Lists template views, optionally scoped to a single type. Filtering by type
/// is delegated to the reader, so the handler forwards the query and returns the
/// views unchanged.
/// </summary>
public sealed class ListTemplatesHandler(ITemplateReader templates)
{
    public async Task<IReadOnlyList<TemplateView>> HandleAsync(
        ListTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await templates.ListAsync(query.Type, cancellationToken);
    }
}

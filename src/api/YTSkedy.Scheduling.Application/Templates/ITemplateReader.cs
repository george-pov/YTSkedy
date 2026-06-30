using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Templates;

public interface ITemplateReader
{
    /// <summary>
    /// Reads a single template view by type and id, or null when no template in
    /// that type partition has the id.
    /// </summary>
    Task<TemplateView?> GetAsync(
        TemplateType type,
        string templateId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads template views. When <paramref name="type"/> is supplied the read
    /// is scoped to that type's partition; when it is null templates of every
    /// type are returned. The returned order is not significant.
    /// </summary>
    Task<IReadOnlyList<TemplateView>> ListAsync(
        TemplateType? type,
        CancellationToken cancellationToken);
}

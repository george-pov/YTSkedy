using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

public sealed record RenderContentResult(
    RenderContentStatus Status,
    RenderedContent? Content,
    bool HasUnresolvedPlaceholders)
{
    public static RenderContentResult Rendered(
        RenderedContent content,
        bool hasUnresolvedPlaceholders)
    {
        ArgumentNullException.ThrowIfNull(content);

        return new RenderContentResult(
            RenderContentStatus.Rendered,
            content,
            hasUnresolvedPlaceholders);
    }

    public static RenderContentResult EmptyTitle(bool hasUnresolvedPlaceholders) =>
        new(RenderContentStatus.EmptyTitle, null, hasUnresolvedPlaceholders);

    public static RenderContentResult TemplateNotFound() =>
        new(RenderContentStatus.TemplateNotFound, null, HasUnresolvedPlaceholders: false);
}

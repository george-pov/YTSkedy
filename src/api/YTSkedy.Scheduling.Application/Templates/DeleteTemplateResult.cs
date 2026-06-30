namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Result of the delete template use case as seen by the HTTP host.
/// <c>Deleted</c> maps to 204, <c>NotFound</c> to 404 when no template has the
/// id, and <c>ReferencedByPlatform</c> to 409 when a platform still links to
/// the template.
/// </summary>
public enum DeleteTemplateResult
{
    Deleted,
    NotFound,
    ReferencedByPlatform
}

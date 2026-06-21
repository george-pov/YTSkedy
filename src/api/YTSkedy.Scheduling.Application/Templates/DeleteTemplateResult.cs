namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Result of the delete template use case as seen by the HTTP host.
/// <c>Deleted</c> maps to 204 and <c>NotFound</c> to 404 when no template has
/// the id.
/// </summary>
public enum DeleteTemplateResult
{
    Deleted,
    NotFound
}

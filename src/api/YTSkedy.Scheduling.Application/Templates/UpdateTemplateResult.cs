namespace YTSkedy.Scheduling.Application.Templates;

/// <summary>
/// Result of the update template use case as seen by the HTTP host.
/// <c>Updated</c> maps to 200, <c>NotFound</c> to 404, and
/// <c>NameAlreadyExists</c> to 409 because another template in the type already
/// uses the new name.
/// </summary>
public enum UpdateTemplateResult
{
    Updated,
    NotFound,
    NameAlreadyExists
}

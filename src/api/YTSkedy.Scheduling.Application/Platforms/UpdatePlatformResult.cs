namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Result of the update platform use case as seen by the HTTP host.
/// <c>Updated</c> maps to 200, <c>NotFound</c> to 404, and
/// <c>NameAlreadyExists</c> to 409 because another platform already uses the new
/// name. <c>Conflict</c> maps to 409 and is reserved for when the platform has a
/// publication row that is still <c>Publishing</c>; that guard is wired once
/// platform publication state exists, so the current handler does not produce
/// it.
/// </summary>
public enum UpdatePlatformResult
{
    Updated,
    NotFound,
    NameAlreadyExists,
    Conflict
}

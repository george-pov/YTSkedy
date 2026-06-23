namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Result of the delete platform use case as seen by the HTTP host.
/// <c>Deleted</c> maps to 204 and <c>NotFound</c> to 404 when no platform has
/// the id. <c>Conflict</c> maps to 409 and is reserved for when the platform has
/// a publication row that is still <c>Publishing</c>; that guard, along with
/// preserving published rows as orphan history, is wired once platform
/// publication state exists, so the current handler does not produce it.
/// </summary>
public enum DeletePlatformResult
{
    Deleted,
    NotFound,
    Conflict
}

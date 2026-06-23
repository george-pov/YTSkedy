namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Single platform shape returned by the list, get, create, and update routes.
/// Carries the persisted id and type, so a client always has what the update and
/// delete routes need.
/// </summary>
public sealed record PlatformResponse(
    string PlatformId,
    string Name,
    string Type,
    PublishSettingsResponse PublishSettings);

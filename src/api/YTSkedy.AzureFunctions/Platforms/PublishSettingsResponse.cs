namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Publish-settings object returned in platform responses. The current slice
/// returns YouTube publish settings. No secret material is included.
/// </summary>
internal sealed record PublishSettingsResponse(
    string Credentials,
    string PrivacyStatus,
    bool SelfDeclaredMadeForKids);

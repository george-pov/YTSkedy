namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Publish-settings object carried by platform create and update requests.
/// Fields are nullable so the API boundary can return <c>400 Bad Request</c> for
/// missing values rather than failing deserialization. The current slice
/// supports YouTube publish settings; <see cref="SelfDeclaredMadeForKids"/>
/// defaults to <c>false</c> when omitted.
/// </summary>
internal sealed record PublishSettingsPayload(
    string? Credentials,
    string? PrivacyStatus,
    bool? SelfDeclaredMadeForKids);

namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Publish-settings object carried by platform create and update requests.
/// Fields are nullable so the API boundary can return <c>400 Bad Request</c>
/// for missing values rather than failing deserialization. The concrete
/// settings fields are selected by the platform type.
/// </summary>
internal sealed record PublishSettingsPayload(
    YouTubeCredentialsPayload? Credentials,
    string? PrivacyStatus,
    bool? SelfDeclaredMadeForKids,
    string? SiteUrl,
    string? Username,
    string? ApplicationPassword,
    string? PostStatus,
    bool? Sticky = null,
    int? ScheduleOffsetHours = null,
    IReadOnlyList<long>? CategoryIds = null);

internal sealed record YouTubeCredentialsPayload(
    string? ClientId,
    string? ClientSecret,
    string? RefreshToken);

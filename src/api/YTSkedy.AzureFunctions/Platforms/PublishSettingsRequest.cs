namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Nested publish-settings request used by platform create and update requests.
/// Fields are nullable so the API boundary can return <c>400 Bad Request</c>
/// for missing values rather than failing deserialization. The concrete
/// settings fields are selected by the platform type.
/// </summary>
internal sealed record PublishSettingsRequest(
    YouTubeCredentialsRequest? Credentials,
    string? PrivacyStatus,
    bool? SelfDeclaredMadeForKids,
    string? SiteUrl,
    string? Username,
    string? ApplicationPassword,
    string? PostStatus,
    bool? Sticky = null,
    int? ScheduleOffsetHours = null,
    IReadOnlyList<long>? CategoryIds = null,
    string? CategoryId = null,
    bool? ContainsSyntheticMedia = null,
    string? DefaultAudioLanguage = null,
    string? DefaultLanguage = null);

internal sealed record YouTubeCredentialsRequest(
    string? ClientId,
    string? ClientSecret,
    string? RefreshToken);

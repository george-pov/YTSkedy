using System.Text.Json.Serialization;

namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// Publish-settings object returned in platform responses. The fields are
/// provider-specific and no secret material is included.
/// </summary>
internal sealed record PublishSettingsResponse(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    YouTubeCredentialsResponse? Credentials,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? PrivacyStatus,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? SelfDeclaredMadeForKids,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? SiteUrl,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Username,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? PostStatus,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? ApplicationPasswordConfigured);

internal sealed record YouTubeCredentialsResponse(
    string ClientId,
    bool ClientSecretConfigured,
    bool RefreshTokenConfigured);

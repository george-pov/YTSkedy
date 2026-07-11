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
    bool? Sticky,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? ScheduleOffsetHours,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<long>? CategoryIds,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? ApplicationPasswordConfigured,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? PasswordDisplayValue)
{
    internal static PublishSettingsResponse ForYouTube(
        YouTubeCredentialsResponse credentials,
        string privacyStatus,
        bool selfDeclaredMadeForKids) =>
        new(
            credentials,
            privacyStatus,
            selfDeclaredMadeForKids,
            SiteUrl: null,
            Username: null,
            PostStatus: null,
            Sticky: null,
            ScheduleOffsetHours: null,
            CategoryIds: null,
            ApplicationPasswordConfigured: null,
            PasswordDisplayValue: null);

    internal static PublishSettingsResponse ForWordPress(
        string siteUrl,
        string username,
        string postStatus,
        bool sticky,
        int? scheduleOffsetHours,
        IReadOnlyList<long> categoryIds,
        bool applicationPasswordConfigured,
        string? passwordDisplayValue) =>
        new(
            Credentials: null,
            PrivacyStatus: null,
            SelfDeclaredMadeForKids: null,
            SiteUrl: siteUrl,
            Username: username,
            PostStatus: postStatus,
            Sticky: sticky,
            ScheduleOffsetHours: scheduleOffsetHours,
            CategoryIds: categoryIds,
            ApplicationPasswordConfigured: applicationPasswordConfigured,
            PasswordDisplayValue: passwordDisplayValue);
}

internal sealed record YouTubeCredentialsResponse(
    string ClientId,
    bool ClientSecretConfigured,
    bool RefreshTokenConfigured,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? ClientSecretDisplayValue,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? RefreshTokenDisplayValue);

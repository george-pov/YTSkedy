using System.Text.Json;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Platforms;

/// <summary>
/// Serializes <see cref="PublishSettings"/> to and from the
/// <c>PublishSettingsJson</c> column. The concrete settings type is selected by
/// the platform's <see cref="PlatformType"/>. Platform rows can store
/// secret-bearing provider settings, while publication snapshots must use
/// <see cref="SerializeSnapshot"/> so provider secrets are not copied into
/// platform-publication rows.
/// </summary>
internal static class PublishSettingsSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    internal static string Serialize(PlatformType type, PublishSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return type switch
        {
            PlatformType.YouTube => JsonSerializer.Serialize(AsYouTube(settings), Options),
            PlatformType.WordPress => JsonSerializer.Serialize(AsWordPress(settings), Options),
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unknown platform type.")
        };
    }

    internal static PublishSettings Deserialize(PlatformType type, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return type switch
        {
            PlatformType.YouTube => DeserializeYouTube(json),
            PlatformType.WordPress => DeserializeWordPress(json),
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unknown platform type.")
        };
    }

    internal static string SerializeSnapshot(PlatformType type, PublishSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return type switch
        {
            PlatformType.YouTube => JsonSerializer.Serialize(
                YouTubeSnapshot.From(AsYouTube(settings)),
                Options),
            PlatformType.WordPress => JsonSerializer.Serialize(
                WordPressSnapshot.From(AsWordPress(settings)),
                Options),
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unknown platform type.")
        };
    }

    internal static PublicationTargetSnapshot? DeserializeSnapshot(PlatformType type, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            return type switch
            {
                PlatformType.YouTube => DeserializeYouTubeSnapshot(root),
                PlatformType.WordPress => DeserializeWordPressSnapshot(root),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static YouTubeSettings DeserializeYouTube(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<YouTubeSettings>(json, Options)
                ?? throw new InvalidOperationException(
                    "Platform row has null publish settings JSON.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Platform row has malformed publish settings JSON.",
                exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                "Platform row has invalid publish settings.",
                exception);
        }
    }

    private static WordPressSettings DeserializeWordPress(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<WordPressSettings>(json, Options)
                ?? throw new InvalidOperationException(
                    "Platform row has null publish settings JSON.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Platform row has malformed publish settings JSON.",
                exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                "Platform row has invalid publish settings.",
                exception);
        }
    }

    private static YouTubeSettings AsYouTube(PublishSettings settings) =>
        settings as YouTubeSettings
            ?? throw new ArgumentException(
                "A YouTube platform requires YouTube publish settings.",
                nameof(settings));

    private static WordPressSettings AsWordPress(PublishSettings settings) =>
        settings as WordPressSettings
            ?? throw new ArgumentException(
                "A WordPress platform requires WordPress publish settings.",
                nameof(settings));

    private static PublicationTargetSnapshot? DeserializeYouTubeSnapshot(JsonElement root)
    {
        if (!root.TryGetProperty("credentials", out var credentials) ||
            !credentials.TryGetProperty("clientId", out var clientId) ||
            string.IsNullOrWhiteSpace(clientId.GetString()))
        {
            return null;
        }

        return new PublicationTargetSnapshot(
            PlatformType.YouTube,
            WordPressSiteUrl: null,
            YouTubeClientId: clientId.GetString()!.Trim());
    }

    private static PublicationTargetSnapshot? DeserializeWordPressSnapshot(JsonElement root)
    {
        if (!root.TryGetProperty("siteUrl", out var siteUrl) ||
            string.IsNullOrWhiteSpace(siteUrl.GetString()))
        {
            return null;
        }

        return new PublicationTargetSnapshot(
            PlatformType.WordPress,
            WordPressSiteUrl: siteUrl.GetString()!.Trim(),
            YouTubeClientId: null);
    }

    private sealed record YouTubeSnapshot(
        YouTubeCredentialsSnapshot Credentials,
        string PrivacyStatus,
        bool SelfDeclaredMadeForKids,
        string? CategoryId = null,
        bool ContainsSyntheticMedia = false)
    {
        internal static YouTubeSnapshot From(YouTubeSettings settings) =>
            new(
                YouTubeCredentialsSnapshot.From(settings.Credentials),
                settings.PrivacyStatus,
                settings.SelfDeclaredMadeForKids,
                settings.CategoryId,
                settings.ContainsSyntheticMedia);
    }

    private sealed record YouTubeCredentialsSnapshot(
        string ClientId,
        bool ClientSecretConfigured,
        bool RefreshTokenConfigured)
    {
        internal static YouTubeCredentialsSnapshot From(YouTubeCredentials credentials) =>
            new(
                credentials.ClientId,
                YouTubeCredentials.IsValidClientSecret(credentials.ClientSecret),
                YouTubeCredentials.IsValidRefreshToken(credentials.RefreshToken));
    }

    private sealed record WordPressSnapshot(
        string SiteUrl)
    {
        internal static WordPressSnapshot From(WordPressSettings settings) =>
            new(settings.SiteUrl);
    }
}

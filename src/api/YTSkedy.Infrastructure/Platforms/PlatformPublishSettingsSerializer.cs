using System.Text.Json;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Platforms;

/// <summary>
/// Serializes <see cref="PublishSettings"/> to and from the
/// <c>PublishSettingsJson</c> column. The concrete settings type is selected by
/// the platform's <see cref="PlatformType"/>. Only non-secret settings are
/// stored; credential material is a reference name resolved outside storage, so
/// no token, secret, or raw authorization header is ever written here.
/// </summary>
internal static class PlatformPublishSettingsSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(PlatformType type, PublishSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return type switch
        {
            PlatformType.YouTube => JsonSerializer.Serialize(AsYouTube(settings), Options),
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unknown platform type.")
        };
    }

    public static PublishSettings Deserialize(PlatformType type, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return type switch
        {
            PlatformType.YouTube => DeserializeYouTube(json),
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unknown platform type.")
        };
    }

    private static YouTubePublishSettings DeserializeYouTube(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<YouTubePublishSettings>(json, Options)
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

    private static YouTubePublishSettings AsYouTube(PublishSettings settings) =>
        settings as YouTubePublishSettings
            ?? throw new ArgumentException(
                "A YouTube platform requires YouTube publish settings.",
                nameof(settings));
}

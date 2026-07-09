using Microsoft.AspNetCore.Mvc;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Platforms;

internal static class PublishSettingsMapper
{
    private const int YouTubeDisplayLength = 12;
    private const int YouTubeSuffixLength = 3;
    private const int WordPressDisplayLength = 7;
    private const int WordPressSuffixLength = 0;
    private const char MaskCharacter = '*';

    internal static PlatformType TypeOf(PublishSettings publishSettings) =>
        publishSettings switch
        {
            YouTubeSettings => PlatformType.YouTube,
            WordPressSettings => PlatformType.WordPress,
            _ => throw new ArgumentOutOfRangeException(
                nameof(publishSettings),
                publishSettings.GetType().Name,
                "Unknown publish settings type.")
        };

    internal static PublishSettingsResponse ToResponse(PublishSettings publishSettings) =>
        publishSettings switch
        {
            YouTubeSettings youTube => PublishSettingsResponse.ForYouTube(
                ToYouTubeCredentialsResponse(youTube.Credentials),
                youTube.PrivacyStatus,
                youTube.SelfDeclaredMadeForKids),
            WordPressSettings wordPress => ToWordPressResponse(wordPress),
            _ => throw new ArgumentOutOfRangeException(
                nameof(publishSettings),
                publishSettings.GetType().Name,
                "Unknown publish settings type.")
        };

    private static PublishSettingsResponse ToWordPressResponse(WordPressSettings wordPress)
    {
        var applicationPasswordConfigured =
            WordPressSettings.IsValidApplicationPassword(wordPress.ApplicationPassword);

        return PublishSettingsResponse.ForWordPress(
            wordPress.SiteUrl,
            wordPress.Username,
            wordPress.PostStatus,
            wordPress.Sticky,
            wordPress.ScheduleOffsetHours,
            applicationPasswordConfigured,
            applicationPasswordConfigured
                ? RedactSecret(
                    wordPress.ApplicationPassword,
                    WordPressDisplayLength,
                    WordPressSuffixLength,
                    MaskCharacter)
                : null);
    }

    internal static string? RedactSecret(
        string? value,
        int displayLength,
        int visibleSuffixLength,
        char maskCharacter)
    {
        if (displayLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayLength),
                displayLength,
                "Display length must be greater than zero.");
        }

        if (visibleSuffixLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(visibleSuffixLength),
                visibleSuffixLength,
                "Visible suffix length must be zero or greater.");
        }

        if (visibleSuffixLength > displayLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(visibleSuffixLength),
                visibleSuffixLength,
                "Visible suffix length must not exceed display length.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var mask = new string(maskCharacter, displayLength);
        if (visibleSuffixLength <= 0 || value.Length < visibleSuffixLength)
        {
            return mask;
        }

        return string.Concat(
            mask.AsSpan(0, displayLength - visibleSuffixLength),
            value.AsSpan(value.Length - visibleSuffixLength));
    }

    internal static bool TryBuild(
        PlatformType type,
        PublishSettingsPayload? payload,
        out PublishSettings publishSettings,
        out IActionResult error) =>
        TryBuild(type, payload, currentSettings: null, out publishSettings, out error);

    internal static bool TryBuild(
        PlatformType type,
        PublishSettingsPayload? payload,
        PublishSettings? currentSettings,
        out PublishSettings publishSettings,
        out IActionResult error)
    {
        publishSettings = default!;
        error = new EmptyResult();

        if (payload is null)
        {
            error = new BadRequestObjectResult("Publish settings are required.");
            return false;
        }

        return type switch
        {
            PlatformType.YouTube => TryBuildYouTubeSettings(
                payload,
                currentSettings as YouTubeSettings,
                out publishSettings,
                out error),
            PlatformType.WordPress => TryBuildWordPressSettings(
                payload,
                currentSettings as WordPressSettings,
                out publishSettings,
                out error),
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unknown platform type.")
        };
    }

    private static YouTubeCredentialsResponse ToYouTubeCredentialsResponse(
        YouTubeCredentials credentials)
    {
        var clientSecretConfigured = YouTubeCredentials.IsValidClientSecret(credentials.ClientSecret);
        var refreshTokenConfigured = YouTubeCredentials.IsValidRefreshToken(credentials.RefreshToken);

        return new(
            credentials.ClientId,
            clientSecretConfigured,
            refreshTokenConfigured,
            clientSecretConfigured
                ? RedactSecret(
                    credentials.ClientSecret,
                    YouTubeDisplayLength,
                    YouTubeSuffixLength,
                    MaskCharacter)
                : null,
            refreshTokenConfigured
                ? RedactSecret(
                    credentials.RefreshToken,
                    YouTubeDisplayLength,
                    YouTubeSuffixLength,
                    MaskCharacter)
                : null);
    }

    private static bool TryBuildYouTubeSettings(
        PublishSettingsPayload payload,
        YouTubeSettings? currentSettings,
        out PublishSettings publishSettings,
        out IActionResult error)
    {
        publishSettings = default!;
        error = new EmptyResult();

        if (payload.Credentials is null)
        {
            error = MissingYouTubeCredentialsResult();
            return false;
        }

        if (!YouTubeCredentials.IsValidClientId(payload.Credentials.ClientId))
        {
            error = MissingYouTubeClientIdResult();
            return false;
        }

        var clientSecret = payload.Credentials.ClientSecret;
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            if (currentSettings is null)
            {
                error = MissingYouTubeClientSecretResult();
                return false;
            }

            clientSecret = currentSettings.Credentials.ClientSecret;
        }

        var refreshToken = payload.Credentials.RefreshToken;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            if (currentSettings is null)
            {
                error = MissingYouTubeRefreshTokenResult();
                return false;
            }

            refreshToken = currentSettings.Credentials.RefreshToken;
        }

        var credentials = new YouTubeCredentials(
            payload.Credentials.ClientId!,
            clientSecret,
            refreshToken);

        if (!YouTubeSettings.IsValidPrivacyStatus(payload.PrivacyStatus))
        {
            error = InvalidPrivacyStatusResult();
            return false;
        }

        publishSettings = new YouTubeSettings(
            credentials,
            payload.PrivacyStatus!,
            payload.SelfDeclaredMadeForKids ?? false);
        return true;
    }

    private static bool TryBuildWordPressSettings(
        PublishSettingsPayload payload,
        WordPressSettings? currentSettings,
        out PublishSettings publishSettings,
        out IActionResult error)
    {
        publishSettings = default!;
        error = new EmptyResult();

        if (!TryValidateWordPressSiteUrl(payload.SiteUrl, out error))
        {
            return false;
        }

        if (!WordPressSettings.IsValidUsername(payload.Username))
        {
            error = InvalidWordPressUsernameResult();
            return false;
        }

        var applicationPassword = payload.ApplicationPassword;
        if (string.IsNullOrWhiteSpace(applicationPassword))
        {
            if (currentSettings is null)
            {
                error = MissingWordPressApplicationPasswordResult();
                return false;
            }

            applicationPassword = currentSettings.ApplicationPassword;
        }

        if (!WordPressSettings.IsValidPostStatus(payload.PostStatus))
        {
            error = InvalidWordPressPostStatusResult();
            return false;
        }

        var scheduleOffsetValidation = WordPressSettings.ValidateScheduleOffsetHours(
            payload.PostStatus,
            payload.ScheduleOffsetHours);
        if (scheduleOffsetValidation != WordPressScheduleOffsetValidationResult.Valid)
        {
            error = ScheduleOffsetHoursResult(scheduleOffsetValidation);
            return false;
        }

        publishSettings = new WordPressSettings(
            payload.SiteUrl!,
            payload.Username!,
            applicationPassword,
            payload.PostStatus!,
            payload.Sticky ?? false,
            payload.ScheduleOffsetHours);
        return true;
    }

    private static bool TryValidateWordPressSiteUrl(string? siteUrl, out IActionResult error)
    {
        error = new EmptyResult();

        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            error = MissingWordPressSiteUrlResult();
            return false;
        }

        if (!Uri.TryCreate(siteUrl.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not "http" and not "https" ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            error = InvalidWordPressSiteUrlResult();
            return false;
        }

        if (uri.Scheme == "http" &&
            !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
            uri.Host != "127.0.0.1")
        {
            error = InsecureWordPressSiteUrlResult();
            return false;
        }

        return true;
    }

    private static IActionResult MissingYouTubeCredentialsResult() =>
        new BadRequestObjectResult("Publish settings credentials are required.");

    private static IActionResult MissingYouTubeClientIdResult() =>
        new BadRequestObjectResult("Publish settings credentials client ID is required.");

    private static IActionResult MissingYouTubeClientSecretResult() =>
        new BadRequestObjectResult("Publish settings credentials client secret is required.");

    private static IActionResult MissingYouTubeRefreshTokenResult() =>
        new BadRequestObjectResult("Publish settings credentials refresh token is required.");

    private static IActionResult InvalidPrivacyStatusResult() =>
        new BadRequestObjectResult(
            "Publish settings privacy status must be 'private', 'public', or 'unlisted'.");

    private static IActionResult MissingWordPressSiteUrlResult() =>
        new BadRequestObjectResult("Publish settings site URL is required.");

    private static IActionResult InvalidWordPressSiteUrlResult() =>
        new BadRequestObjectResult(
            "Publish settings site URL must be an absolute HTTP(S) URL without credentials.");

    private static IActionResult InsecureWordPressSiteUrlResult() =>
        new BadRequestObjectResult(
            "Publish settings site URL must use HTTPS unless it targets localhost or 127.0.0.1.");

    private static IActionResult InvalidWordPressUsernameResult() =>
        new BadRequestObjectResult("Publish settings username is required.");

    private static IActionResult MissingWordPressApplicationPasswordResult() =>
        new BadRequestObjectResult("Publish settings Application Password is required.");

    private static IActionResult InvalidWordPressPostStatusResult() =>
        new BadRequestObjectResult(
            "Publish settings post status must be 'draft', 'pending', 'private', 'future', or 'publish'.");

    private static IActionResult ScheduleOffsetHoursResult(
        WordPressScheduleOffsetValidationResult validation) =>
        validation switch
        {
            WordPressScheduleOffsetValidationResult.Missing => MissingScheduleOffsetHoursResult(),
            WordPressScheduleOffsetValidationResult.Unsupported => UnsupportedScheduleOffsetHoursResult(),
            WordPressScheduleOffsetValidationResult.NonPositive or
                WordPressScheduleOffsetValidationResult.AboveMaximum =>
                    InvalidScheduleOffsetHoursResult(),
            _ => new BadRequestObjectResult("Publish settings schedule offset hours are invalid.")
        };

    private static IActionResult MissingScheduleOffsetHoursResult() =>
        new BadRequestObjectResult(
            "Publish settings schedule offset hours must be provided when post status is 'future'.");

    private static IActionResult UnsupportedScheduleOffsetHoursResult() =>
        new BadRequestObjectResult(
            "Publish settings schedule offset hours must be omitted unless post status is 'future'.");

    private static IActionResult InvalidScheduleOffsetHoursResult() =>
        new BadRequestObjectResult(
            $"Publish settings schedule offset hours must be between 1 and {WordPressSettings.MaxScheduleOffsetHours}.");
}

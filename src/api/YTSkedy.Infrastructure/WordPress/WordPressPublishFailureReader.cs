using System.Net;
using System.Text;
using System.Text.Json;
using YTSkedy.Scheduling.Application.Platforms.Providers;

namespace YTSkedy.Infrastructure.WordPress;

/// <summary>
/// Converts a failed WordPress create-post response into bounded, secret-safe
/// diagnostic details. Raw response bodies and provider messages do not leave
/// this type.
/// </summary>
internal static class WordPressPublishFailureReader
{
    private const int MaximumResponseCharacters = 8192;
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    internal static async Task<PlatformPublishFailure> ReadAsync(
        HttpResponseMessage response,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(timeProvider);

        string? providerErrorCode = null;
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) is true)
        {
            try
            {
                var responsePrefix = await ReadResponsePrefixAsync(
                    response.Content,
                    cancellationToken);
                var providerError = JsonSerializer.Deserialize<WordPressErrorResponse>(
                    responsePrefix,
                    JsonOptions);
                providerErrorCode = NormalizeProviderErrorCode(providerError?.Code);
            }
            catch (JsonException)
            {
                // Keep the status-based diagnostic. The raw body is neither
                // logged nor persisted when the provider shape is invalid.
            }
        }

        var statusCode = response.StatusCode;
        return new PlatformPublishFailure(
            FailureCodeFor(statusCode),
            FailureMessageFor(statusCode),
            "create_post",
            (int)statusCode,
            providerErrorCode,
            GetRetryAfterUtc(response, timeProvider),
            VerificationRequired: true);
    }

    private static DateTimeOffset? GetRetryAfterUtc(
        HttpResponseMessage response,
        TimeProvider timeProvider)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Date is not null)
        {
            return retryAfter.Date.Value;
        }

        return retryAfter?.Delta is null
            ? null
            : timeProvider.GetUtcNow() + retryAfter.Delta.Value;
    }

    private static string FailureCodeFor(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.TooManyRequests =>
                PlatformPublishFailureCodes.WordPressRateLimited,
            HttpStatusCode.Unauthorized =>
                PlatformPublishFailureCodes.WordPressAuthenticationFailed,
            HttpStatusCode.Forbidden =>
                PlatformPublishFailureCodes.WordPressPermissionDenied,
            HttpStatusCode.BadRequest or HttpStatusCode.Conflict or
                HttpStatusCode.UnprocessableEntity =>
                PlatformPublishFailureCodes.WordPressRequestRejected,
            _ => PlatformPublishFailureCodes.WordPressProviderError
        };

    private static string FailureMessageFor(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.TooManyRequests =>
                "WordPress limited publishing requests.",
            HttpStatusCode.Unauthorized =>
                "WordPress rejected the configured username or application password.",
            HttpStatusCode.Forbidden =>
                "WordPress denied permission to create the post.",
            HttpStatusCode.BadRequest or HttpStatusCode.Conflict or
                HttpStatusCode.UnprocessableEntity =>
                "WordPress rejected the post request.",
            _ => "WordPress returned an error while creating the post."
        };

    private static async Task<string> ReadResponsePrefixAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: false);
        var buffer = new char[MaximumResponseCharacters];
        var count = 0;
        while (count < buffer.Length)
        {
            var read = await reader.ReadAsync(
                buffer.AsMemory(count, buffer.Length - count),
                cancellationToken);
            if (read == 0)
            {
                break;
            }

            count += read;
        }

        return new string(buffer, 0, count);
    }

    private static string? NormalizeProviderErrorCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = new string(
            value.Trim()
                .Take(100)
                .Where(character =>
                    char.IsAsciiLetterOrDigit(character) ||
                    character is '_' or '-' or '.')
                .ToArray());
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private sealed record WordPressErrorResponse(string? Code);
}

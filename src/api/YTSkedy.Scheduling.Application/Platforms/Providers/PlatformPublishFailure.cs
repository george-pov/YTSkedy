namespace YTSkedy.Scheduling.Application.Platforms.Providers;

/// <summary>
/// Secret-safe provider failure details that can cross the application
/// boundary and be persisted for operator troubleshooting.
/// </summary>
public sealed record PlatformPublishFailure(
    string Code,
    string Message,
    string Stage,
    int? ProviderStatus = null,
    string? ProviderErrorCode = null,
    DateTimeOffset? RetryAfterUtc = null,
    bool VerificationRequired = true);

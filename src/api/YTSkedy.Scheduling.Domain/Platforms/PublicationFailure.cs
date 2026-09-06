namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Secret-safe diagnostic summary for the most recent failed publication
/// attempt. Provider response bodies and request content are never stored.
/// </summary>
public sealed record PublicationFailure(
    string Code,
    string Message,
    string Stage,
    int? ProviderStatus,
    string? ProviderErrorCode,
    DateTimeOffset? RetryAfterUtc,
    DateTimeOffset FailedUtc,
    string? AttemptId,
    bool VerificationRequired);

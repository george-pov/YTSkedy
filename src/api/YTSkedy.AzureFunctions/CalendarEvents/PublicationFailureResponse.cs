namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// Secret-safe failure details retained with a failed publication row.
/// </summary>
internal sealed record PublicationFailureResponse(
    string Code,
    string Message,
    string Stage,
    int? ProviderStatus,
    string? ProviderErrorCode,
    DateTimeOffset? RetryAfterUtc,
    DateTimeOffset FailedUtc,
    string? AttemptId,
    bool VerificationRequired);

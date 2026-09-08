namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// Stable machine-readable error for publication actions that need specific
/// operator guidance in the UI.
/// </summary>
internal sealed record PublicationActionErrorResponse(
    string Code,
    string Message,
    string? Stage = null,
    int? ProviderStatus = null,
    string? ProviderErrorCode = null,
    DateTimeOffset? RetryAfterUtc = null,
    string? AttemptId = null,
    bool VerificationRequired = false);

namespace YTSkedy.Scheduling.Application.Platforms.Providers;

/// <summary>
/// Raised by an <see cref="IPlatformPublisher"/> when the external publish fails,
/// including when the provider's credentials are not configured. The publish use
/// case records a secret-safe failure that the HTTP boundary maps to an
/// actionable provider status. When
/// the provider created a resource before a later step failed,
/// <see cref="ExternalResourceId"/> carries its safe provider id for operator
/// troubleshooting. The message must never contain secrets, tokens, or raw
/// provider credentials.
/// </summary>
public sealed class PlatformPublishException : Exception
{
    public PlatformPublishException(string message)
        : this(
            message,
            externalResourceId: null,
            PlatformPublishFailureKind.ProviderFailure,
            innerException: null)
    {
    }

    public PlatformPublishException(string message, Exception innerException)
        : this(
            message,
            externalResourceId: null,
            PlatformPublishFailureKind.ProviderFailure,
            innerException)
    {
    }

    public PlatformPublishException(
        string message,
        string? externalResourceId,
        Exception? innerException = null)
        : this(
            message,
            externalResourceId,
            PlatformPublishFailureKind.ProviderFailure,
            innerException)
    {
    }

    public PlatformPublishException(
        string message,
        string? externalResourceId,
        PlatformPublishFailureKind failureKind,
        Exception? innerException = null,
        PlatformPublishFailure? failure = null)
        : base(message, innerException)
    {
        ExternalResourceId = string.IsNullOrWhiteSpace(externalResourceId)
            ? null
            : externalResourceId.Trim();
        FailureKind = failureKind;
        Failure = failure ?? CreateDefaultFailure(message, failureKind);
    }

    public PlatformPublishException(
        PlatformPublishFailure failure,
        string? externalResourceId = null,
        PlatformPublishFailureKind failureKind = PlatformPublishFailureKind.ProviderFailure,
        Exception? innerException = null)
        : this(
            failure?.Message ?? throw new ArgumentNullException(nameof(failure)),
            externalResourceId,
            failureKind,
            innerException,
            failure)
    {
    }

    public string? ExternalResourceId { get; }

    public PlatformPublishFailureKind FailureKind { get; }

    public PlatformPublishFailure Failure { get; }

    private static PlatformPublishFailure CreateDefaultFailure(
        string message,
        PlatformPublishFailureKind failureKind) =>
        new(
            failureKind switch
            {
                PlatformPublishFailureKind.Timeout =>
                    PlatformPublishFailureCodes.ProviderTimeout,
                PlatformPublishFailureKind.UnexpectedCancellation =>
                    PlatformPublishFailureCodes.ProviderCanceled,
                _ => PlatformPublishFailureCodes.ProviderFailure
            },
            message,
            "provider");
}
